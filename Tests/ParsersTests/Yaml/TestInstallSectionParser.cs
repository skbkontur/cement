using Common;
using Common.YamlParsers;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.ParsersTests.Yaml
{
    [TestFixture]
    public class TestInstallSectionParser
    {
        [TestCaseSource(nameof(ArtifactsSource))]
        [TestCaseSource(nameof(BuildFilesSource))]
        public void Parse(string[] install, string[] artifacts, string[] artefacts, InstallData expected)
        {
            var parser = new InstallSectionParser();
            var actual = parser.Parse(install, artifacts, artefacts);

            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] ArtifactsSource =
        {
            new TestCaseData(null, new[] { "a1", "a2" }, new[] { "a3", "a4" },
                    new InstallData
                    {
                        Artifacts = { "a1", "a2", "a3", "a4" },
                        CurrentConfigurationInstallFiles = { "a1", "a2", "a3", "a4" }
                    })
                .SetName("Install section: artifacts. Multiple artifacts"),

            new TestCaseData(null, new[] { "a1" }, new[] { "a1" },
                    new InstallData
                    {
                        Artifacts = { "a1" },
                        CurrentConfigurationInstallFiles = { "a1" }
                    })
                .SetName("Install section: artifacts. Artifacts are not duplicated."),

            new TestCaseData(new[] { "a1" }, new[] { "a2" }, new[] { "a3" },
                    new InstallData
                    {
                        InstallFiles = { "a1" },
                        Artifacts = { "a1", "a2", "a3" },
                        CurrentConfigurationInstallFiles = { "a1", "a2", "a3" }
                    })
                .SetName("Install section: artifacts. Install files from install section are added to artifacts"),

            new TestCaseData(new[]
                    {
                        "a1",
                        "nuget Nuget1"
                    }, new[]
                    {
                        "a2",
                        "nuget Nuget2"
                    }, new[]
                    {
                        "a3",
                        "nuget Nuget3"
                    },
                    new InstallData
                    {
                        InstallFiles = { "a1" },
                        Artifacts = { "a1", "a2", "a3" },
                        CurrentConfigurationInstallFiles = { "a1", "a2", "a3" },
                        NuGetPackages = { "Nuget1" }
                    })
                .SetName("Install section: artifacts. Nugets are not added to artifacts."),

            new TestCaseData(new[]
                    {
                        "a1",
                        "module Module1"
                    }, new[]
                    {
                        "a2",
                        "module Module2"
                    }, new[]
                    {
                        "a3",
                        "module Module3"
                    },
                    new InstallData
                    {
                        InstallFiles = { "a1" },
                        Artifacts = { "a1", "a2", "a3" },
                        CurrentConfigurationInstallFiles = { "a1", "a2", "a3" },
                        ExternalModules = { "Module1" }
                    })
                .SetName("Install section: artifacts. External modules are not added to artifacts."),
        };

        private static TestCaseData[] BuildFilesSource =
        {
            new TestCaseData(
                    new[]
                    {
                        "file1",
                        "nuget Nuget1",
                        "module Module1",
                    },
                    new[]
                    {
                        "file2",
                        "nuget Nuget2",
                        "module Module2",
                    },
                    new[]
                    {
                        "file3",
                        "nuget Nuget3",
                        "module Module3",
                    },
                    new InstallData
                    {
                        InstallFiles = { "file1" },
                        Artifacts = { "file1", "file2", "file3" },
                        CurrentConfigurationInstallFiles = { "file1", "file2", "file3" },
                        ExternalModules = { "Module1" },
                        NuGetPackages = { "Nuget1" }
                    })
                .SetName("Install section: install files. Nuget and external modules are not considered install files."),
        };
    }
}