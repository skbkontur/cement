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
        public void Parse(YamlInstallSections yamlInstallSection, InstallData expected)
        {
            var parser = new InstallSectionParser();
            var actual = parser.Parse(yamlInstallSection);
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [TestCaseSource(nameof(MergeCases))]
        public void Merge(YamlInstallSections yamlInstallSection, InstallData defaults, InstallData[] parentInstalls, InstallData expected)
        {
            var parser = new InstallSectionParser();
            var actual = parser.Parse(yamlInstallSection, defaults, parentInstalls);
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] ArtifactsSource =
        {
            new TestCaseData(
                    new YamlInstallSections(new string[0], new[] {"a1", "a2"}, new[] {"a3", "a4"}),
                    new InstallData
                    {
                        Artifacts = {"a1", "a2", "a3", "a4"},
                        CurrentConfigurationInstallFiles = {"a1", "a2", "a3", "a4"}
                    })
                .SetName("Install section: artifacts. Multiple artifacts"),

            new TestCaseData(
                    new YamlInstallSections(new string[0], new[] {"a1"}, new[] {"a1"}),
                    new InstallData
                    {
                        Artifacts = {"a1"},
                        CurrentConfigurationInstallFiles = {"a1"}
                    })
                .SetName("Install section: artifacts. Artifacts are not duplicated."),

            new TestCaseData(
                    new YamlInstallSections(new[] {"a1"}, new[] {"a2"}, new[] {"a3"}),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2", "a3"},
                        CurrentConfigurationInstallFiles = {"a1", "a2", "a3"}
                    })
                .SetName("Install section: artifacts. Install files from install section are added to artifacts"),

            new TestCaseData(
                    new YamlInstallSections(
                        new[]
                        {
                            "a1",
                            "nuget Nuget1"
                        },
                        new[]
                        {
                            "a2",
                            "nuget Nuget2"
                        },
                        new[]
                        {
                            "a3",
                            "nuget Nuget3"
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2", "a3"},
                        CurrentConfigurationInstallFiles = {"a1", "a2", "a3"},
                        NuGetPackages = {"Nuget1"}
                    })
                .SetName("Install section: artifacts. Nugets are not added to artifacts."),

            new TestCaseData(
                    new YamlInstallSections(
                        new[]
                        {
                            "a1",
                            "module Module1"
                        },
                        new[]
                        {
                            "a2",
                            "module Module2"
                        },
                        new[]
                        {
                            "a3",
                            "module Module3"
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"a1"},
                        Artifacts = {"a1", "a2", "a3"},
                        CurrentConfigurationInstallFiles = {"a1", "a2", "a3"},
                        ExternalModules = {"Module1"}
                    })
                .SetName("Install section: artifacts. External modules are not added to artifacts."),
        };

        private static TestCaseData[] BuildFilesSource =
        {
            new TestCaseData(
                    new YamlInstallSections(
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
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"file1"},
                        Artifacts = {"file1", "file2", "file3"},
                        CurrentConfigurationInstallFiles = {"file1", "file2", "file3"},
                        ExternalModules = {"Module1"},
                        NuGetPackages = {"Nuget1"}
                    })
                .SetName("Install section: install files. Nuget and external modules are not considered install files."),
        };

        private static TestCaseData[] MergeCases =
        {
            new TestCaseData(
                    new YamlInstallSections(
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
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"default_file"},
                        Artifacts = {"default_artifact"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact"},
                        ExternalModules = {"default_module"},
                        NuGetPackages = {"default_nuget"}
                    },
                    null,
                    new InstallData
                    {
                        InstallFiles = {"default_file", "file1"},
                        Artifacts = {"default_artifact", "file1", "file2", "file3"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact", "file1", "file2", "file3"},
                        ExternalModules = {"default_module", "Module1"},
                        NuGetPackages = {"default_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from 'default' section."),

            new TestCaseData(
                    new YamlInstallSections(
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
                        }
                    ),
                    null,
                    new[]
                    {
                        new InstallData
                        {
                            InstallFiles = {"parent1_file"},
                            Artifacts = {"parent1_artifact"},
                            CurrentConfigurationInstallFiles = {"parent1_file", "parent1_artifact"},
                            ExternalModules = {"parent1_module"},
                            NuGetPackages = {"parent1_nuget"}
                        },
                        new InstallData
                        {
                            InstallFiles = {"parent2_file"},
                            Artifacts = {"parent2_artifact"},
                            CurrentConfigurationInstallFiles = {"parent2_file", "parent2_artifact"},
                            ExternalModules = {"parent2_module"},
                            NuGetPackages = {"parent2_nuget"}
                        },
                    },
                    new InstallData
                    {
                        InstallFiles = {"parent1_file", "parent2_file", "file1"},
                        Artifacts = {"parent1_artifact", "parent2_artifact", "file1", "file2", "file3"},
                        CurrentConfigurationInstallFiles = {"parent1_file", "parent1_artifact", "parent2_file", "parent2_artifact", "file1", "file2", "file3"},
                        ExternalModules = {"parent1_module", "parent2_module", "Module1"},
                        NuGetPackages = {"parent1_nuget", "parent2_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from several parent configurations."),

            new TestCaseData(
                    new YamlInstallSections(
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
                        }
                    ),
                    new InstallData
                    {
                        InstallFiles = {"default_file"},
                        Artifacts = {"default_artifact"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact"},
                        ExternalModules = {"default_module"},
                        NuGetPackages = {"default_nuget"}
                    },
                    new[]
                    {
                        new InstallData
                        {
                            InstallFiles = {"parent1_file"},
                            Artifacts = {"parent1_artifact"},
                            CurrentConfigurationInstallFiles = {"parent1_file", "parent1_artifact"},
                            ExternalModules = {"parent1_module"},
                            NuGetPackages = {"parent1_nuget"}
                        },
                        new InstallData
                        {
                            InstallFiles = {"parent2_file"},
                            Artifacts = {"parent2_artifact"},
                            CurrentConfigurationInstallFiles = {"parent2_file", "parent2_artifact"},
                            ExternalModules = {"parent2_module"},
                            NuGetPackages = {"parent2_nuget"}
                        },
                    },
                    new InstallData
                    {
                        InstallFiles = {"default_file", "parent1_file", "parent2_file", "file1"},
                        Artifacts = {"default_artifact", "parent1_artifact", "parent2_artifact", "file1", "file2", "file3"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact", "parent1_file", "parent1_artifact", "parent2_file", "parent2_artifact", "file1", "file2", "file3"},
                        ExternalModules = {"default_module", "parent1_module", "parent2_module", "Module1"},
                        NuGetPackages = {"default_nuget", "parent1_nuget", "parent2_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from 'default' section and several parent configurations."),
        };
    }
}