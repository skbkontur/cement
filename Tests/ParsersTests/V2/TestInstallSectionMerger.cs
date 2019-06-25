using Common;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestInstallSectionMerger
    {
        [TestCaseSource(nameof(MergeCases))]
        public void Merge(InstallSection installSection, InstallData defaults, InstallData[] parentInstalls, InstallData expected)
        {
            var parser = new InstallSectionParser();
            var merger = new InstallSectionMerger();

            var currentConfigInstallData = parser.Parse(installSection, defaults?.CurrentConfigurationInstallFiles);
            var actual = merger.Merge(currentConfigInstallData, defaults, parentInstalls);
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] MergeCases =
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
                        Artifacts = {"default_artifact", "file1", "file2"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact", "file1", "file2"},
                        ExternalModules = {"default_module", "Module1"},
                        NuGetPackages = {"default_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from 'default' section."),

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
                        Artifacts = {"parent1_artifact", "parent2_artifact", "file1", "file2"},
                        CurrentConfigurationInstallFiles = {"file1", "file2"},
                        ExternalModules = {"parent1_module", "parent2_module", "Module1"},
                        NuGetPackages = {"parent1_nuget", "parent2_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from several parent configurations."),

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
                        Artifacts = {"default_artifact", "parent1_artifact", "parent2_artifact", "file1", "file2"},
                        CurrentConfigurationInstallFiles = {"default_file", "default_artifact", "file1", "file2"},
                        ExternalModules = {"default_module", "parent1_module", "parent2_module", "Module1"},
                        NuGetPackages = {"default_nuget", "parent1_nuget", "parent2_nuget", "Nuget1"}
                    })
                .SetName("Install section: install files. Current config inherits Install section from 'default' section and several parent configurations."),
        };
    }
}