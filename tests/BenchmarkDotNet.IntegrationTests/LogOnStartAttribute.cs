////using System.Reflection;

////namespace BenchmarkDotNet.IntegrationTests;

////public class LogOnStartAttribute : BeforeAfterTestAttribute
////{
////    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
////    {
////        Console.WriteLine($"[START] {methodUnderTest.DeclaringType?.FullName}.{methodUnderTest.Name}");
////    }

////    public override void After(MethodInfo methodUnderTest, IXunitTest test)
////    {
////        Console.WriteLine($"[END] {methodUnderTest.DeclaringType?.FullName}.{methodUnderTest.Name}");
////    }
////}
