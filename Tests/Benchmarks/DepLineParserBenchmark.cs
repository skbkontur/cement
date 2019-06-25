using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Common;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using NUnit.Framework;

namespace Tests.Benchmarks
{
    [TestFixture]
    [Explicit]
    public class DepLineParserBenchmarkTest
    {
        [Test]
        public void Test() => BenchmarkRunner.Run<DepLineParserBenchmark>();
    }

    [Config(typeof(Config))]
    public class DepLineParserBenchmark
    {
        private DepSectionItemParser parser;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(StatisticColumn.P85,
                    StatisticColumn.P90,
                    StatisticColumn.P95,
                    StatisticColumn.P100);
            }
        }

        [ParamsSource(nameof(DepLines))]
        public string DepLine;

        [GlobalSetup]
        public void Setup()
        {
            parser = new DepSectionItemParser();
        }

        [Benchmark]
        public DepSectionItem Parse1() => parser.Parse(DepLine);

        [Benchmark]
        public Dep Parse2() => new Dep(DepLine);

        public IEnumerable<string> DepLines => new[]
        {
            "module",
            "module@b32742e9701aef44ee986db2824e9007056ba60f/some-cfg",
            "module/some-cfg@b32742e9701aef44ee986db2824e9007056ba60f",
            @"module@hello\/there\@general/kenobi",
        };
    }
}