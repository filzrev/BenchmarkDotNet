using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Loggers;

internal sealed class Broker : IDisposable
{
    private readonly ILogger logger;
    private readonly Process process;
    private readonly CompositeInProcessDiagnoser compositeInProcessDiagnoser;
    private readonly AnonymousPipeServerStream inputFromBenchmark;
    private readonly AnonymousPipeServerStream acknowledgments;

    // Used to stop the pipeline when the process exits or AfterAll is received
    private readonly CancellationTokenSource cts = new();

    // Temporary variabled that are used to parse InProcessDiagnoser data.
    private readonly StringBuilder pendingResultsBuilder = new();
    private int pendingDiagnoserIndex = -1;
    private int pendingResultsLines;

    private bool needFlushWriter = false;
    private bool disposed;

    private static readonly byte[] AckLineBytes = AnonymousPipesHost.UTF8NoBOM.GetBytes(Engine.Signals.Acknowledgment + Environment.NewLine);

    public Broker(
        ILogger logger,
        Process process,
        IDiagnoser? diagnoser,
        CompositeInProcessDiagnoser compositeInProcessDiagnoser,
        BenchmarkCase benchmarkCase,
        BenchmarkId benchmarkId,
        AnonymousPipeServerStream inputFromBenchmark,
        AnonymousPipeServerStream acknowledgments)
    {
        this.logger = logger;
        this.process = process;
        Diagnoser = diagnoser;
        this.compositeInProcessDiagnoser = compositeInProcessDiagnoser;
        this.inputFromBenchmark = inputFromBenchmark;
        this.acknowledgments = acknowledgments;

        DiagnoserActionParameters = new DiagnoserActionParameters(process, benchmarkCase, benchmarkId);

        process.EnableRaisingEvents = true;
        process.Exited += OnProcessExited;
    }

    internal IDiagnoser? Diagnoser { get; }

    internal DiagnoserActionParameters DiagnoserActionParameters { get; }

    internal List<string> Results { get; } = [];

    internal List<string> PrefixedOutput { get; } = [];

    internal void ProcessData()
        => ProcessDataAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Starts processing data from the benchmark process using PipeReader/PipeWriter.
    /// This method completes when the pipe reaches EOF or cancellation is requested.
    /// </summary>
    internal async Task ProcessDataAsync(CancellationToken cancellationToken = default)
    {
        // Usually, this property is not set yet.
        // If the process has already exited, the pipe may never produce data.
        if (process.HasExited)
            return;

        var reader = PipeReader.Create(inputFromBenchmark);
        var writer = PipeWriter.Create(acknowledgments);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        try
        {
            await ReadLoopAsync(reader, writer, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore exception.
        }
        finally
        {
            await reader.CompleteAsync();
            await writer.CompleteAsync();
        }
    }

    /// <summary>
    /// Continuously reads from the PipeReader, extracts line-based messages and process line.
    /// </summary>
    private async ValueTask ReadLoopAsync(PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
    {
        while (true)
        {
            ReadResult read = await reader.ReadAsync(cancellationToken);

            var buffer = read.Buffer;

            // Consume all complete lines currently available in the buffer
            while (TryReadLine(ref buffer, out var line))
            {
                HandleLine(line, writer);

                if (needFlushWriter)
                {
                    await writer.FlushAsync(cancellationToken);
                    needFlushWriter = false;
                }
            }

            // Advance the reader to the consumed position
            reader.AdvanceTo(buffer.Start, buffer.End);

            // Exit when the writer has completed and no more data will arrive
            if (read.IsCompleted)
                return;
        }
    }

    /// <summary>
    /// Handles a single logical line coming from the benchmark process.
    /// </summary>
    private void HandleLine(string line, PipeWriter writer)
    {
        // TODO: implement Silent mode here
        logger.WriteLine(LogKind.Default, line);

        // Handle normal benchmark log.
        if (!line.StartsWith("//"))
        {
            Results.Add(line);
            return;
        }

        // Handle InProcessDiagnoser header and results lines.
        if (line.StartsWith(CompositeInProcessDiagnoser.HeaderKey))
        {
            HandleDiagnoserDataHeaderLine(line);
            return;
        }

        // Handle other signals
        if (Engine.Signals.TryGetSignal(line, out var signal))
        {
            Diagnoser?.Handle(signal, DiagnoserActionParameters);

            // Write acknowledgment bytes.
            writer.Write(AckLineBytes);
            needFlushWriter = true;

            switch (signal)
            {
                case HostSignal.BeforeAnythingElse:
                    // The client has connected, we no longer need to keep the local copy of client handle alive.
                    // This allows server to detect that child process is done and hence avoid resource leak.
                    // Full explanation: https://stackoverflow.com/a/39700027
                    inputFromBenchmark.DisposeLocalCopyOfClientHandle();
                    acknowledgments.DisposeLocalCopyOfClientHandle();
                    return;

                case HostSignal.AfterAll:
                    // we have received the last signal so we can stop reading from the pipe
                    // if the process won't exit after this, its hung and needs to be killed
                    cts.Cancel();
                    return;

                default:
                    break;
            }
        }

        // Handle other "//" prefixed line.
        if (!string.IsNullOrEmpty(line))
            PrefixedOutput.Add(line);
    }

    private void HandleDiagnoserDataHeaderLine(string line)
    {
        // Keep in sync with WasmExecutor and InProcessHost.

        // Handle InProcessDiagnoser header line
        if (line.StartsWith(CompositeInProcessDiagnoser.HeaderKey + " "))
        {
            if (pendingResultsLines > 0)
                throw new InvalidDataException($"{CompositeInProcessDiagnoser.HeaderKey} line detected during the processing of another diagnoser's data.{pendingResultsLines}");

            // Example: "// InProcessDiagnoser 0 1"
            var items = line.Split([' ']);
            if (items.Length < 4)
                throw new InvalidDataException(line);

            // Parse data from splitted items.
            if (!int.TryParse(items[2], out pendingDiagnoserIndex) ||
                !int.TryParse(items[3], out pendingResultsLines))
                throw new InvalidDataException(line);

            return;
        }

        // Handle InProcessDiagnoserResults line
        if (line.StartsWith(CompositeInProcessDiagnoser.ResultsKey))
        {
            // Strip the prepended "// InProcessDiagnoserResults ".
            var payload = line.Substring(CompositeInProcessDiagnoser.ResultsKey.Length + 1);

            pendingResultsBuilder.Append(payload);

            if (--pendingResultsLines > 0)
            {
                pendingResultsBuilder.AppendLine();
                return;
            }

            var results = pendingResultsBuilder.ToString();

            // Deserialize results.
            compositeInProcessDiagnoser.DeserializeResults(
                pendingDiagnoserIndex,
                DiagnoserActionParameters.BenchmarkCase,
                results);

            // Reset state.
            pendingResultsBuilder.Clear();
            pendingDiagnoserIndex = -1;

            return;
        }

        // This line is not expected to be called.
        throw new InvalidDataException(line);
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out string? line)
    {
        var lf = buffer.PositionOf((byte)'\n');
        if (lf is null)
        {
            line = null;
            return false;
        }

        // Gets line text.
        var lineBytes = buffer.Slice(0, buffer.GetPosition(1, lf.Value));

        // Update buffer
        buffer = buffer.Slice(buffer.GetPosition(1, lf.Value));

#if NET6_0_OR_GREATER
        line = AnonymousPipesHost.UTF8NoBOM.GetString(lineBytes);
#else
        line = AnonymousPipesHost.UTF8NoBOM.GetString(lineBytes.ToArray());
#endif

        line = line.TrimEnd(['\r', '\n']);

        return true;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (disposed)
            return;

        process.Exited -= OnProcessExited;
        cts.Cancel();
    }

    /// <summary>
    /// Releases all resources used by this instance
    /// </summary>
    /// <remarks>
    /// Don't call this method asynchronously.
    /// Currently it must be called after processing finished.
    /// </remarks>
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Dispose();

        // Dispose all the pipes to let reading from pipe finish with EOF and avoid a reasource leak.
        inputFromBenchmark.DisposeLocalCopyOfClientHandle();
        inputFromBenchmark.Dispose();
        acknowledgments.DisposeLocalCopyOfClientHandle();
        acknowledgments.Dispose();
    }
}
