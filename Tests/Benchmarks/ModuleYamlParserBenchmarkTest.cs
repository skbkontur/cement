using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace Tests.Benchmarks;

[TestFixture]
[Explicit]
public class ModuleYamlParserBenchmarkTest
{
    [Test]
    public void Test() => BenchmarkRunner.Run<ModuleYamlParserBenchmark>();
}
