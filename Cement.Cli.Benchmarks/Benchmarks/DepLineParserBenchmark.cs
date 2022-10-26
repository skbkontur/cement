using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Common;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using JetBrains.Annotations;

namespace Cement.Cli.Benchmarks.Benchmarks;

[PublicAPI]
[Config(typeof(Config))]
public class DepLineParserBenchmark
{
    [ParamsSource(nameof(DepLines))]
    public string? DepLine { get; set; }
    private DepSectionItemParser? parser;

    public IEnumerable<string> DepLines => new[]
    {
        "module",
        "module@b32742e9701aef44ee986db2824e9007056ba60f/some-cfg",
        "module/some-cfg@b32742e9701aef44ee986db2824e9007056ba60f",
        // ReSharper disable once StringLiteralTypo
        @"module@hello\/there\@general/kenobi"
    };

    [GlobalSetup]
    public void Setup()
    {
        parser = new DepSectionItemParser();
    }

    [Benchmark]
    public DepSectionItem Parse1() => parser!.Parse(DepLine);

    [Benchmark]
    public Dep Parse2() => new(DepLine);

    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(
                StatisticColumn.P85,
                StatisticColumn.P90,
                StatisticColumn.P95,
                StatisticColumn.P100);
        }
    }
}
