using System;
using System.Collections.Generic;
using Common;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestBuildSectionParser
    {
        [TestCaseSource(nameof(GoodDefaultsCasesSource))]
        public void ParseBuildDefaultsSections(string input, BuildData expected)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseDefaults(buildSections);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCaseSource(nameof(GoodConfigurationCasesSource))]
        public void ParseBuildConfigurationSections(string input, BuildData[] expected)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseConfiguration(buildSections);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCaseSource(nameof(GoodConfigurationCasesWithDefaultsSource))]
        public void ParseBuildConfigurationSections(string input, BuildData defaults, BuildData[] expected)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseConfiguration(buildSections, defaults);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCase("msbuild", null, null, ExpectedResult = null)]
        [TestCase("msbuild", null, "default_version", ExpectedResult = "default_version")]
        [TestCase("msbuild", "version_from_settings", "default_version", ExpectedResult = "default_version")]
        [TestCase("msbuild", "version_from_settings", null, ExpectedResult = "version_from_settings")]
        [TestCase("msbuild", "version_from_settings", "", ExpectedResult = "version_from_settings")]
        [TestCase("not_msbuild", null, null, ExpectedResult = null)]
        [TestCase("not_msbuild", null, "default_version", ExpectedResult = "default_version")]
        [TestCase("not_msbuild", "version_from_settings", "default_version", ExpectedResult = "default_version")]
        [TestCase("not_msbuild", "version_from_settings", null, ExpectedResult = null)]
        [TestCase("not_msbuild", "version_from_settings", "", ExpectedResult = null)]
        public string DefineCorrectDefaultToolVersion(string toolName, string settingVersion, string defaultVersion)
        {
            var settings = new CementSettings { DefaultMsBuildVersion = settingVersion };
            var input = $@"build:
  target: Solution.sln
  configuration: Release
  tool:
    name: {toolName}
";
            var defaults = new BuildData(null, null, new Tool(null, defaultVersion), new List<string>(), string.Empty);

            var parser = new BuildSectionParser(settings);
            var buildSections = GetBuildSections(input);
            var actual = parser.ParseConfiguration(buildSections, defaults);
            return actual?[0].Tool.Version;
        }

        [TestCaseSource(nameof(BadCasesSource))]
        public void ThrowOnInvalidBuildSection(string input, Type expectedException)
        {
            var parser = new BuildSectionParser();
            var buildSections = GetBuildSections(input);
            Assert.Throws(expectedException, () => parser.ParseConfiguration(buildSections));
        }

        private static TestCaseData[] GoodConfigurationCasesSource =
        {
            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: Release",
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), string.Empty),
                    })
                .SetName("Single sln-target with configuration, no params, no tool, no name"),

            new TestCaseData(
                    @"build:
  - name: build-name
    target: Solution.sln
    configuration: Release",
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), "build-name"),
                    })
                .SetName("Single sln-target with configuration, no params, no tool, with name"),

            new TestCaseData(
                @"build:
  target: Solution.sln
  configuration: Debug
  tool:
    name: sometool
    version: ""14.0""",
                new[]
                {
                    new BuildData("Solution.sln", "Debug", new Tool("sometool", "14.0"), new List<string>(), string.Empty),
                }).SetName("Single sln-target with configuration, no params, multiline tool, no name"),

            new TestCaseData(
                @"build:
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

            new TestCaseData(
                @"build:
  target: package.json",
                new[]
                {
                    new BuildData("package.json", null, new Tool("msbuild"), new List<string>(), string.Empty),
                }).SetName("Single package.json-target, no configuration, no params, default tool, no name"),

            new TestCaseData(
                @"build:
  target: package.json
  tool: npm",
                new[]
                {
                    new BuildData("package.json", null, new Tool("npm"), new List<string>(), string.Empty),
                }).SetName("Single package.json-target, no configuration, no params, single tool, no name"),

            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: Release
  parameters: ""/p:WarningLevel=0""",
                    new[]
                    {
                        new BuildData(
                            "Solution.sln",
                            "Release",
                            new Tool("msbuild"),
                            new List<string>
                            {
                                "/p:WarningLevel=0"
                            },
                            string.Empty),
                    })
                .SetName("Single sln-target with configuration, single-line params, no tool, no name"),

            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: Release
  parameters:
    - ""/p:WarningLevel=0""
    - ""/p:SomeParam1=0""
    - ""/p:SomeParam2=0""",
                    new[]
                    {
                        new BuildData(
                            "Solution.sln",
                            "Release",
                            new Tool("msbuild"),
                            new List<string>
                            {
                                "/p:WarningLevel=0",
                                "/p:SomeParam1=0",
                                "/p:SomeParam2=0"
                            },
                            string.Empty),
                    })
                .SetName("Single sln-target with configuration, multiline params in quotes, no tool, no name"),

            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: Release
  parameters:
    - /p:WarningLevel=0
    - /p:SomeParam1=0
    - /p:SomeParam2=0",
                    new[]
                    {
                        new BuildData(
                            "Solution.sln",
                            "Release",
                            new Tool("msbuild"),
                            new List<string>
                            {
                                "/p:WarningLevel=0",
                                "/p:SomeParam1=0",
                                "/p:SomeParam2=0"
                            },
                            string.Empty),
                    })
                .SetName("Single sln-target with configuration, multiline params without quotes, no tool, no name"),

            new TestCaseData(
                    @"build:
  - name: Utilities
    target: utilities.sln
    configuration: Release
    parameters: ""/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp""

  - name: ""Transport Log""
    target: transportLog.sln
    configuration: Release",
                    new[]
                    {
                        new BuildData(
                            "utilities.sln",
                            "Release",
                            new Tool("msbuild"),
                            new List<string>
                            {
                                "/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp",
                            },
                            "Utilities"),

                        new BuildData("transportLog.sln", "Release", new Tool("msbuild"), new List<string>(), "Transport Log"),
                    })
                .SetName("Multi sln-target with configuration, single-line params, no tool"),

            new TestCaseData(
                    @"build:
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
                        new BuildData(
                            "utilities.sln",
                            "Debug",
                            new Tool("sometool", "14.0"),
                            new List<string>
                            {
                                "/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp",
                            },
                            "Utilities"),

                        new BuildData(
                            @"PSModules\PSMoiraWorks\deps.ps1",
                            "Release",
                            new Tool("powershell"),
                            new List<string>()
                            {
                                "-NonInteractive -NoProfile -ExecutionPolicy Bypass"
                            },
                            "Restore deps for PSMoiraWorks"),
                    })
                .SetName("Multi sln-powershell-target with configuration, single-line params, with multi- and signle-line tool"),
        };

        private static TestCaseData[] GoodConfigurationCasesWithDefaultsSource =
        {
            new TestCaseData(
                    @"build:
    configuration: Release",
                    new BuildData("Solution.sln", null, null, new List<string>(), string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), string.Empty),
                    })
                .SetName("Single build section. Sln-target from defaults"),

            new TestCaseData(
                    @"build:
    target: Solution.sln",
                    new BuildData(null, "Release", null, new List<string>(), string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), string.Empty),
                    })
                .SetName("Single build section. Configuration from defaults"),

            new TestCaseData(
                    @"build:
    target: Solution.sln
    configuration: Release",
                    new BuildData(null, null, new Tool(null, "42"), new List<string>(), string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild", "42"), new List<string>(), string.Empty),
                    })
                .SetName("Single build section. Build tool version from defaults"),

            new TestCaseData(
                    @"build:
    target: Solution.sln
    configuration: Release",
                    new BuildData(null, null, new Tool("sometool"), new List<string>(), string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("sometool"), new List<string>(), string.Empty),
                    })
                .SetName("Single build section. Build tool name from defaults"),


            new TestCaseData(
                    @"build:
    target: Solution.sln
    configuration: Release",
                    new BuildData(null, null, null, new List<string>() {"param1", "param2"}, string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>() {"param1", "param2"}, string.Empty),
                    })
                .SetName("Single build section. Build parameters from defaults"),

            new TestCaseData(
                    @"build:
    target: Solution.sln
    configuration: Release",
                    new BuildData(null, null, null, new List<string>(), "buildname"),
                    new[]
                    {
                        new BuildData("Solution.sln", "Release", new Tool("msbuild"), new List<string>(), "buildname"),
                    })
                .SetName("Single build section. Build parameters from defaults"),

            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: Release
  parameters: ""/p:WarningLevel=0""",
                    new BuildData(null, null, null, new List<string>() {"param1", "param2"}, string.Empty),
                    new[]
                    {
                        new BuildData(
                            "Solution.sln",
                            "Release",
                            new Tool("msbuild"),
                            new List<string> { "/p:WarningLevel=0" },
                            string.Empty),
                    })
                .SetName("Single build section. Build params from default are overriden, not added."),

            new TestCaseData(
                    @"build:
  - name: debug
    configuration: Debug

  - name: release
    configuration: Release
",
                    new BuildData("Solution.sln", null, new Tool("sometool", "42"), new List<string>(), string.Empty),
                    new[]
                    {
                        new BuildData("Solution.sln", "Debug", new Tool("sometool", "42"), new List<string> (), "debug"),
                        new BuildData("Solution.sln", "Release", new Tool("sometool", "42"), new List<string> (), "release"),
                    })
                .SetName("Multiple build section. Default values added to every section"),

            new TestCaseData(
                    @"build:
  - name: Utilities
    target: utilities.sln
    configuration: Debug
    parameters: ""/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp""
    tool:
      name: sometool
      version: ""14.0""

  - name: ""Restore deps for PSMoiraWorks""
    parameters: -NonInteractive -NoProfile -ExecutionPolicy Bypass
    target: PSModules\PSMoiraWorks\deps.ps1
    configuration: Release
    tool:
      name: sometool
      version: ""14.0"""
                    ,
                    new BuildData("Default.sln", "Default", new Tool("default_name", "default_version"), new List<string>() {"default"}, "default"),
                    new[]
                    {
                        new BuildData(
                            "utilities.sln",
                            "Debug",
                            new Tool("sometool", "14.0"),
                            new List<string>
                            {
                                "/t:Rebuild /nodeReuse:false /maxcpucount /v:m /p:WarningLevel=0;DeployOnBuild=true;PublishProfile=WebApp",
                            },
                            "Utilities"),

                        new BuildData(
                            @"PSModules\PSMoiraWorks\deps.ps1",
                            "Release",
                            new Tool("sometool", "14.0"),
                            new List<string>()
                            {
                                "-NonInteractive -NoProfile -ExecutionPolicy Bypass"
                            },
                            "Restore deps for PSMoiraWorks"),
                    })
                .SetName("Multiple build section. Explicit values from config override default values."),

        };

        private static TestCaseData[] GoodDefaultsCasesSource =
        {
            new TestCaseData(
                    @"
build:
  target: Solution.sln",
                    new BuildData("Solution.sln", null, new Tool("msbuild"), new List<string>(), string.Empty)
                )
                .SetName("'configuration' is not required for *.sln targets in default section"),
        };

        private static TestCaseData[] BadCasesSource =
        {
            new TestCaseData(
                    @"build:
  - target: Solution.sln
    configuration: debug
  - target: Pollution.sln
    configuration: release", typeof(CementException))
                .SetName("Multiple parts of build-section require names"),

            new TestCaseData(
                    @"build:
  target: Solution.sln
  configuration: debug
  tool:  ", typeof(BadYamlException))
                .SetName("Tool can't be an empty string"),
        };

        private object GetBuildSections(string text)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>)serializer.Deserialize(text);

            return yaml["build"];
        }
    }
}