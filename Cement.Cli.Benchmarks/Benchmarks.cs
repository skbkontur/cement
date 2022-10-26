// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Cement.Cli.Benchmarks.Benchmarks;

switch (args[0])
{
    case "dep":
        BenchmarkRunner.Run<DepLineParserBenchmark>();
        break;
    case "module":
        BenchmarkRunner.Run<ModuleYamlParserBenchmark>();
        break;
    default:
        BenchmarkRunner.Run<DepLineParserBenchmark>();
        BenchmarkRunner.Run<ModuleYamlParserBenchmark>();
        break;
}
