using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DepLineParserBenchmark>();
        }
    }
}