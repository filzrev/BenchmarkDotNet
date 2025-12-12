using System;
using System.Threading;

#nullable enable

namespace BenchmarkDotNet.Engines
{
    public struct ThreadingStats : IEquatable<ThreadingStats>
    {
        internal const string ResultsLinePrefix = "// Threading: ";

        public static ThreadingStats Empty => new ThreadingStats(0, 0, 0);

        public long CompletedWorkItemCount { get; }
        public long LockContentionCount { get; }
        public long TotalOperations { get; }

        public ThreadingStats(long completedWorkItemCount, long lockContentionCount, long totalOperations)
        {
            CompletedWorkItemCount = completedWorkItemCount;
            LockContentionCount = lockContentionCount;
            TotalOperations = totalOperations;
        }

        public bool Equals(ThreadingStats other) => CompletedWorkItemCount == other.CompletedWorkItemCount && LockContentionCount == other.LockContentionCount && TotalOperations == other.TotalOperations;

        public override bool Equals(object? obj) => obj is ThreadingStats other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(CompletedWorkItemCount, LockContentionCount, TotalOperations);
    }
}