using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Common.YamlParsers;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2.Factories;
using NUnit.Framework;

namespace Tests.Benchmarks
{
    [TestFixture]
    [Explicit]
    public class ModuleYamlParserBenchmarkTest
    {
        [Test]
        public void Test() => BenchmarkRunner.Run<ModuleYamlParserBenchmark>();
    }

    public class ModuleYamlParserBenchmark
    {
        [ParamsSource(nameof(Yamls))]
        public string Content;

        [Benchmark]
        public ModuleDefinition NewModuleYamlParser()
        {
            var parser = ModuleYamlParserFactory.Get();
            return parser.Parse(Content);
        }

        [Benchmark]
        public Dictionary<string, DepsData> OldDepsParser()
        {
            var parser = new DepsYamlParser("fakename", Content);
            var configs = parser.GetConfigurations();

            return configs.ToDictionary(c => c, c => parser.Get(c));
        }

        public IEnumerable<string> Yamls => new[]
        {
            SmallModuleYaml,
            FatModuleYaml,
        };

        private const string SmallModuleYaml = @"notests *default:
  build:
    target: Vostok.Configuration.Sources.ClusterConfig.sln
    configuration: NoTests
    tool:
      name: dotnet

  install:
    - Vostok.Configuration.Sources.ClusterConfig/bin/Release/netstandard2.0/Vostok.Configuration.Sources.ClusterConfig.dll
    - module vostok.configuration.abstractions
    - module vostok.clusterconfig.client.abstractions

  deps:
    - vostok.devtools
    - vostok.configuration.abstractions
    - vostok.configuration.sources
    - vostok.clusterconfig.client.abstractions

full-build > notests:
  deps:
    - vostok.commons.testing/src

  build:
    target: Vostok.Configuration.Sources.ClusterConfig.sln
    configuration: Release";

        private const string FatModuleYaml = @"default:
  hooks:
    - pre-commit.cement

core *default:
  deps:
    - clusterclient.core
    - zookeeper/client
    - tracing
    - core
    - drive/client
    - zebra/client
    - analyzers.async-code
    - force: $CURRENT_BRANCH
    - jetbrains-annotations
    - logging
    - convert-utf8
    - code-style

  build:
    target: Forms.Core.sln
    configuration: Core

  install:
    - Core\bin\Release\Kontur.Forms.Core.dll

service > core:
  deps:
    - -zookeeper/client
    - zookeeper
    - -zebra/client
    - zebra
    - -logging
    - logging/log4net
    - tracing-cc
    - recognition-httpclient
    - forms.progress
    - libfront
    - config
    - datacenter-utils
    - clusterconfig
    - graphite
    - houston.echelon.worker
    - houston.plugin
    - force: $CURRENT_BRANCH
    - echelon
    - log4net
    - portal
    - topology
    - http

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Core.Service\bin\Release\Kontur.Forms.Core.Service.dll

jobs-core > core:
  deps:
    - tracing-cc
    - forms.progress
    - echelon/client
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: JobsCore

  install:
    - Core.Jobs\bin\Release\Kontur.Forms.Core.Jobs.dll
    - module echelon/client
    - module zebra/client

jobs-service > service, jobs-core:
  deps:
    - -echelon
    - echelon/sdk
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Core.Topshelf\bin\Release\Kontur.Forms.Core.Topshelf.dll
    - Core.Metrics\bin\Release\Kontur.Forms.Core.Metrics.dll
    - Jobs.Service\bin\Release\Kontur.Forms.Core.Jobs.Service.dll
    - module logging/log4net
    - module log4net
    - module config
    - module core
    - module houston.echelon.worker
    - module houston.plugin

jobs-plugin > service, jobs-core:
  deps:
    - -echelon
    - echelon/sdk
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Core.Plugin\bin\Release\Kontur.Forms.Core.Plugin.dll
    - Jobs.Plugin\bin\Release\Kontur.Forms.Jobs.Plugin.dll
    - Core.Metrics\bin\Release\Kontur.Forms.Core.Metrics.dll
    - Jobs.Service\bin\Release\Kontur.Forms.Core.Jobs.Service.dll
    - module logging/log4net
    - module log4net
    - module config
    - module core
    - module houston.echelon.worker
    - module houston.plugin

jobs-service-local > jobs-service:
  deps:
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Jobs.Service.TestHost\bin\Release\Kontur.Forms.Core.Jobs.Service.TestHost.dll
    - module echelon/sdk
    - module zebra/sdk

webapi-service > service:
  deps:
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Core.Topshelf\bin\Release\Kontur.Forms.Core.Topshelf.dll
    - Core.WebApi\bin\Release\Kontur.Forms.Core.WebApi.dll
    - Core.Metrics\bin\Release\Kontur.Forms.Core.Metrics.dll
    - WebApi.Service\bin\Release\Kontur.Forms.Core.WebApi.Service.dll
    - module logging/log4net
    - module log4net
    - module http
    - module config
    - module core
  artifacts:
    - WebApi.Service.Playground\bin\Release\Kontur.Forms.Core.WebApi.Service.Playground.dll

webapi-plugin > service:
  deps:
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - Core.Plugin\bin\Release\Kontur.Forms.Core.Plugin.dll
    - WebApi.Plugin\bin\Release\Kontur.Forms.WebApi.Plugin.dll
    - Core.WebApi\bin\Release\Kontur.Forms.Core.WebApi.dll
    - Core.Metrics\bin\Release\Kontur.Forms.Core.Metrics.dll
    - WebApi.Service\bin\Release\Kontur.Forms.Core.WebApi.Service.dll
    - module logging/log4net
    - module log4net
    - module http
    - module config
    - module core
  artifacts:
    - WebApi.Service.Playground\bin\Release\Kontur.Forms.Core.WebApi.Service.Playground.dll

webapi-service-local > webapi-service:
  deps:
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release

  install:
    - WebApi.Service.TestHost\bin\Release\Kontur.Forms.Core.WebApi.Service.TestHost.dll

full-build > webapi-service-local, jobs-service-local, webapi-plugin, jobs-plugin:
  deps:
    - force: $CURRENT_BRANCH

  build:
    target: Forms.Core.sln
    configuration: Release
";
    }
}