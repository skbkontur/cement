using System.Collections.Generic;
using Common;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;

namespace Tests.ParsersTests.Yaml
{
    [TestFixture]
    public class TestBuildSectionParser
    {
        [TestCaseSource(nameof(GoodConfigurationCasesSource))]
        [TestCaseSource(nameof(GoodDefaultsCasesSource))]
        public void ParseBuildDefaultsSections(string input, BuildData[] expected)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseBuildSections(buildSections);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCaseSource(nameof(GoodConfigurationCasesSource))]
        public void ParseBuildConfigurationSections(string input, BuildData[] expected)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseBuildSections(buildSections);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCaseSource(nameof(BadCasesSource))]
        public void ThrowOnInvalidBuildSection(string input)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            parser.ParseBuildSections(buildSections);
        }

        private static TestCaseData[] GoodConfigurationCasesSource =
        {
            new TestCaseData(@"build:
  target: Solution.sln
  configuration: Release",
                new[]
                {
                    new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), string.Empty),
                })
                .SetName("Single sln-target with configuration, no params, no tool, no name"),

            new TestCaseData(@"build:
  - name: build-name
    target: Solution.sln
    configuration: Release",
                new[]
                {
                    new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), "build-name"),
                })
                .SetName("Single sln-target with configuration, no params, no tool, with name"),

            new TestCaseData(@"build:
  target: Solution.sln
  configuration: Debug
  tool:
    name: sometool
    version: ""14.0""",
                new[]
                {
                    new BuildData("Solution.sln", "Debug", new Tool("sometool", "14.0"), new List<string>(), string.Empty),
                }).SetName("Single sln-target with configuration, no params, multiline tool, no name"),

            new TestCaseData(@"build:
  - name: build-name
    target: Solution.sln
    configuration: Debug
    tool:
      name: sometool
      version: ""14.0""",
                new[]
                {
                    new BuildData("Solution.sln", "Debug", new Tool("sometool", "14.0"), new List<string>(), "build-name"),
                }).SetName("Single sln-target with configuration, no params, multiline tool, with name"),

            new TestCaseData(@"build:
  target: package.json",
                new[]
                {
                    new BuildData("package.json", null, new Tool("msbuild"), new List<string>(), string.Empty),
                }).SetName("Single package.json-target, no configuration, no params, default tool, no name"),

            new TestCaseData(@"build:
  target: package.json
  tool: npm",
                new[]
                {
                    new BuildData("package.json", null, new Tool("npm"), new List<string>(), string.Empty),
                }).SetName("Single package.json-target, no configuration, no params, single tool, no name"),

            new TestCaseData(@"build:
  target: Solution.sln
  configuration: Release
  parameters: ""/p:WarningLevel=0""",
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>
                        {
                            "/p:WarningLevel=0"
                        }, string.Empty),
                    })
                .SetName("Single sln-target with configuration, single-line params, no tool, no name"),

            new TestCaseData(@"build:
  target: Solution.sln
  configuration: Release
  parameters:
    - ""/p:WarningLevel=0""
    - ""/p:SomeParam1=0""
    - ""/p:SomeParam2=0""",
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>
                        {
                            "/p:WarningLevel=0",
                            "/p:SomeParam1=0",
                            "/p:SomeParam2=0"
                        }, string.Empty),
                    })
                .SetName("Single sln-target with configuration, multiline params in quotes, no tool, no name"),

            new TestCaseData(@"build:
  target: Solution.sln
  configuration: Release
  parameters:
    - /p:WarningLevel=0
    - /p:SomeParam1=0
    - /p:SomeParam2=0",
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>
                        {
                            "/p:WarningLevel=0",
                            "/p:SomeParam1=0",
                            "/p:SomeParam2=0"
                        }, string.Empty),
                    })
                .SetName("Single sln-target with configuration, multiline params without quotes, no tool, no name"),

            new TestCaseData(@"build:
  - name: Utilities
    target: utilities.sln
    configuration: Release
    parameters: ""/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp""

  - name: ""Transport Log""
    target: transportLog.sln
    configuration: Release",
                new[]
                {
                    new BuildData("utilities.sln", "Release", new Tool("msbuild"), new List<string>
                    {
                        "/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp",
                    }, "Utilities"),

                    new BuildData("transportLog.sln", "Release", new Tool("msbuild"), new List<string>(), "Transport Log"),
                })
            .SetName("Multi sln-target with configuration, single-line params, no tool"),

            new TestCaseData(@"build:
  - name: Utilities
    target: utilities.sln
    configuration: Debug
    parameters: ""/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp""
    tool:
      name: sometool
      version: ""14.0""

  - name: ""Restore deps for PSMoiraWorks""
    tool: powershell
    parameters: -NonInteractive -NoProfile -ExecutionPolicy Bypass
    target: PSModules\PSMoiraWorks\deps.ps1
    configuration: Release",
                    new[]
                    {
                        new BuildData("utilities.sln", "Debug", new Tool("sometool", "14.0"), new List<string>
                        {
                            "/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp",
                        }, "Utilities"),

                        new BuildData(@"PSModules\PSMoiraWorks\deps.ps1", "Release", new Tool("powershell"), new List<string>()
                            {
                                "-NonInteractive -NoProfile -ExecutionPolicy Bypass"
                            },
                            "Restore deps for PSMoiraWorks"),
                    })
                .SetName("Multi sln-powershell-target with configuration, single-line params, with multi- and signle-line tool"),
        };

        private static TestCaseData[] GoodDefaultsCasesSource =
        {
            new TestCaseData(@"
build:
  target: Solution.sln",
                    new[]
                    {
                        new BuildData("Solution.sln", null, new Tool("msbuild"), new List<string>(), string.Empty),
                    })

                .SetName("'configuration' is not required for *.sln targets in default section"),
        };

        private static TestCaseData[] BadCasesSource =
        {
            /*
            new TestCaseData(@"build:
  target: Solution.sln")
                .Throws(typeof(BadYamlException))
                .SetName("Sln-target require 'configuration'"),
                */

            new TestCaseData(@"build:
  - target: Solution.sln
    configuration: debug
  - target: Pollution.sln
    configuration: release")
                .Throws(typeof(CementException))
                .SetName("Multiple parts of build-section require names"),

            new TestCaseData(@"build:
  target: Solution.sln
  configuration: debug
  tool:  ")
                .Throws(typeof(BadYamlException))
                .SetName("Tool can't be an empty string"),
        };

        private object GetBuildSections(string text)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>) serializer.Deserialize(text);

            return yaml["build"];
        }
    }
}