using Common;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestInstallSectionParser
    {
        [TestCaseSource(nameof(ArtifactsSource))]
        [TestCaseSource(nameof(BuildFilesSource))]
        public void Parse(InstallSection installSection, InstallData expected)
        {
            var parser = new InstallSectionParser();
            var actual = parser.Parse(installSection);
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] ArtifactsSource =
        {
            new TestCaseData(
                    new InstallSection(new string[0], new[] {"a1", "a2"}),
                    new InstallData
                    {
                        Artifacts = {"a1", "a2"},
                        CurrentConfigurationInstallFiles = {"a1", "a2"}
                    })
                .SetName("Install section: artifacts. Multiple artifacts"),

            new TestCaseData(
                    new InstallSection(new string[0], new[] {"a1"}),
                    new InstallData
                    {
                        Artifacts = {"a1"},
                        CurrentConfigurationInstallFiles = {"a1"}
                    })
                .SetName("Install section: artifacts. Artifacts are not duplicated."),

            new TestCaseData(
                    new InstallSection(new[] {"a1"}, new[] {"a2"}),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2"},
                        CurrentConfigurationInstallFiles = {"a1", "a2"}
                    })
                .SetName("Install section: artifacts. Install files from install section are added to artifacts"),

            new TestCaseData(
                    new InstallSection(
                        new[]
                        {
                            "a1",
                            "nuget Nuget1"
                        },
                        new[]
                        {
                            "a2",
                            "nuget Nuget2"
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2"},
                        CurrentConfigurationInstallFiles = {"a1", "a2"},
                        NuGetPackages = {"Nuget1"}
                    })
                .SetName("Install section: artifacts. Nugets are not added to artifacts."),

            new TestCaseData(
                    new InstallSection(
                        new[]
                        {
                            "a1",
                            "module Module1"
                        },
                        new[]
                        {
                            "a2",
                            "module Module2"
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2"},
                        CurrentConfigurationInstallFiles = {"a1", "a2"},
                        ExternalModules = {"Module1"}
                    })
                .SetName("Install section: artifacts. External modules are not added to artifacts."),
        };

        private static TestCaseData[] BuildFilesSource =
        {
            new TestCaseData(
                    new InstallSection(
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
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"file1"},
                        Artifacts = {"file1", "file2"},
                        CurrentConfigurationInstallFiles = {"file1", "file2"},
                        ExternalModules = {"Module1"},
                        NuGetPackages = {"Nuget1"}
                    })
                .SetName("Install section: install files. Nuget and external modules are not considered install files."),
        };
    }
}