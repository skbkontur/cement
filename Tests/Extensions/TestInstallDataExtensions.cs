using System;
using Common;
using System.Collections.Generic;
using Common.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Extensions
{
    [TestFixture]
    public class TestInstallDataExtensions
    {
        private readonly InstallData First = new InstallData
        {
            Artifacts = {"first_a1", "first_a2", "common_a"},
            InstallFiles = {"first_i1", "first_i2", "common_i"},
            CurrentConfigurationInstallFiles = {"first_i1", "first_i2", "common_i", "first_a1", "first_a2", "common_a"},
            ExternalModules = {"first_m1", "first_m2", "common_m"},
            NuGetPackages = {"first_n1", "first_n2", "common_n"},
        };

        private readonly InstallData Second = new InstallData
        {
            Artifacts = {"second_a1", "second_a2", "common_a"},
            InstallFiles = {"second_i1", "second_i2", "common_i"},
            CurrentConfigurationInstallFiles = {"second_i1", "second_i2", "common_i", "second_a1", "second_a2", "common_a"},
            ExternalModules = {"second_m1", "second_m2", "common_m"},
            NuGetPackages = {"second_n1", "second_n2", "common_n"},
        };

        [Test]
        public void JoinWithReturnsANewInstance()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles);
            Assert.False(ReferenceEquals(First, actual) || ReferenceEquals(Second, actual), "New instance of InstallData had to be created");
        }

        [Test]
        public void JoinWithCreatesNewLists()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles);

            ValidateCollectionRefs(actual, i => i.Artifacts);
            ValidateCollectionRefs(actual, i => i.InstallFiles);
            ValidateCollectionRefs(actual, i => i.CurrentConfigurationInstallFiles);
            ValidateCollectionRefs(actual, i => i.ExternalModules);
            ValidateCollectionRefs(actual, i => i.NuGetPackages);
        }

        [Test]
        public void JoinWithCreatesValidArtifactsList()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles).Artifacts;
            var expected = new List<string> {"first_a1", "first_a2", "common_a", "second_a1", "second_a2"};
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void JoinWithCreatesValidInstallFilesList()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles).InstallFiles;
            var expected = new List<string> {"first_i1", "first_i2", "common_i", "second_i1", "second_i2"};
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void JoinWithDoesNotAffectCurrentConfigurationInstallFiles()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles).CurrentConfigurationInstallFiles;
            var expected = new List<string> {"first_i1", "first_i2", "common_i", "first_a1", "first_a2", "common_a"};
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void JoinWithCreatesValidExternalModulesList()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles).ExternalModules;
            var expected = new List<string> {"first_m1", "first_m2", "common_m", "second_m1", "second_m2"};
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void JoinWithCreatesValidNugetPackagesList()
        {
            var actual = First.JoinWith(Second, First.CurrentConfigurationInstallFiles).NuGetPackages;
            var expected = new List<string> {"first_n1", "first_n2", "common_n", "second_n1", "second_n2"};
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private void ValidateCollectionRefs(InstallData actual, Func<InstallData, object> getCollection)
        {
            var firstCollection = getCollection(First);
            var secondCollection = getCollection(Second);
            var actualCollection = getCollection(actual);
            Assert.False(ReferenceEquals(firstCollection, actualCollection) || ReferenceEquals(secondCollection, actualCollection), "New instance of InstallData's collection had to be created");
        }
    }
}