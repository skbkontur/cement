using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers;
using NUnit.Framework;

namespace Tests.ParsersTests.Yaml
{
    [TestFixture]
    public class TestConfigurationHierarchy
    {
        private readonly ConfigLineParser parser;

        public TestConfigurationHierarchy()
        {
            this.parser = new ConfigLineParser();
        }

        [TestCaseSource(nameof(FindAllParentSource))]
        public void FindAllParents(string childConfig, string[] configLines, string[] expectedParents)
        {
            var hierarchy = GetHierarchy(configLines);
            var actualParents = hierarchy.FindAllParents(childConfig);
            Assert.AreEqual(expectedParents, actualParents);
        }

        [TestCaseSource(nameof(FindClosestParentsSource))]
        public void FindClosestParents(string childConfig, string[] configLines, string[] expectedParents)
        {
            var hierarchy = GetHierarchy(configLines);
            var actualParents = hierarchy.FindClosestParents(childConfig);
            Assert.AreEqual(expectedParents, actualParents);
        }

        [TestCaseSource(nameof(FindDefaultSource))]
        public string FindDefault(List<string> configLines)
        {
            var hierarchy = GetHierarchy(configLines);
            return hierarchy.GetDefault();
        }

        [TestCaseSource(nameof(GetAllSource))]
        public void GetAll(string[] configLines, string[] expectedAll)
        {
            var hierarchy = GetHierarchy(configLines);
            var actualAll = hierarchy.GetAll();
            var msg = "Actual: " + string.Join(", ", actualAll)
                                 + Environment.NewLine
                                 + "Expected: " + string.Join(", ", expectedAll);

            Assert.AreEqual(expectedAll, actualAll, msg);
        }

        private TestCaseData[] FindAllParentSource =
        {
            new TestCaseData("A", new[] { "A" }, null)
                .SetName("Single A without parent"),
            new TestCaseData("B", new[]
            {
                "A *default",
                "B > A",
            }, new[] { "A" })
                .SetName("One root, one child"),

            new TestCaseData("C", new[]
            {
                "A *default",
                "B > A",
                "C > A"
            }, new[] { "A" })
                .SetName("One root, 2 children"),

            new TestCaseData("D", new[]
            {
                "A",
                "B > A",
                "C > A",
                "D > A *default",
            }, new[] { "A" })
                .SetName("One root, 2 usual children and one marked as default"),

            new TestCaseData("E", new[]
            {
                "A",
                "B > A",
                "C > A",
                "D > A *default",
                "E > B"
            }, new[] { "B", "A" })
                .SetName("Two roots"),

/*
                A    F
               /|\   |
              B C D' G
              \     /
               E   /
                \ /
                 H
*/
            new TestCaseData("H", new[]
            {
                "A",
                "B > A",
                "C > A",
                "D > A *default",
                "E > B",
                "F",
                "G > F",
                "H > E, G"

            }, new[] { "E", "G", "B", "F", "A" })
                .SetName("Complex tree"),
        };

        private TestCaseData[] FindClosestParentsSource =
        {
            new TestCaseData("A", new[] { "A" }, null)
                .SetName("Single A without parent"),
            new TestCaseData("B", new[]
                {
                    "A *default",
                    "B > A",
                }, new[] { "A" })
                .SetName("One root, one child"),

            new TestCaseData("C", new[]
                {
                    "A *default",
                    "B > A",
                    "C > A"
                }, new[] { "A" })
                .SetName("One root, 2 children"),

            new TestCaseData("D", new[]
                {
                    "A",
                    "B > A",
                    "C > A",
                    "D > A *default",
                }, new[] { "A" })
                .SetName("One root, 2 usual children and one marked as default"),

            new TestCaseData("E", new[]
                {
                    "A",
                    "B > A",
                    "C > A",
                    "D > A *default",
                    "E > B"
                }, new[] { "B" })
                .SetName("Two roots"),

/*
                A    F
               /|\   |
              B C D' G
              \     /
               E   /
                \ /
                 H
*/
            new TestCaseData("H", new[]
                {
                    "A",
                    "B > A",
                    "C > A",
                    "D > A *default",
                    "E > B",
                    "F",
                    "G > F",
                    "H > E, G"

                }, new[] { "E", "G" })
                .SetName("Complex tree"),
        };

        private TestCaseData[] FindDefaultSource =
        {
            new TestCaseData(new List<string> { "A" })
                .Returns("A")
                .SetName("Single config is default"),

            new TestCaseData(new List<string> { "A *default" })
                .Returns("A")
                .SetName("Single config marked as default"),

            new TestCaseData(new List<string>
            {
                "A",
                "B *default",
            })
                .Returns("B")
                .SetName("Two non-full-build configs, second of them marked as default"),

            new TestCaseData(new List<string>
            {
                "A *default",
                "B",
            })
                .Returns("A")
                .SetName("Two non-full-build configs, first of them marked as default"),

            new TestCaseData(new List<string>
            {
                "A",
                "B",
                "full-build"
            })
                .Returns("full-build")
                .SetName("Two non-full-build configs and a full-build, no default mark"),

            new TestCaseData(new List<string>
            {
                "A *default",
                "B",
                "full-build"
            })
                .Returns("A")
                .SetName("Two non-full-build configs and a full-build, first marked as default"),

            new TestCaseData(new List<string>
            {
                "A > B *default",
                "B",
                "full-build"
            })
                .Returns("A")
                .SetName("Two inhereted non-full-build configs and a full-build, first marked as default"),

            new TestCaseData(new List<string> { "A", "B" })
                .Throws(typeof(ArgumentException))
                .SetName("Throw ArgumentException on several non-full-build configs without default mark"),
        };

        private TestCaseData[] GetAllSource =
        {
            new TestCaseData(new[] { "A" }, new[] { "A" })
                .SetName("A"),

            new TestCaseData(new[]
                {
                    "A",
                    "B *default"
                }, new[] { "A", "B" })
                .SetName("A; B"),

            new TestCaseData(new[]
                {
                    "A",
                    "B *default",
                    "C > A"

                }, new[] { "A", "B", "C" })
                .SetName("A; B; C > A"),


/*
              A   D
             / \ /
            B'  C
 */
            new TestCaseData(new[]
                {
                    "A",
                    "B > A *default",
                    "C > A, D",
                    "D",

                }, new[] { "A", "D", "B", "C" })
                .SetName("A; D; B>A; C>A,D"),

            /*
                A    F
               /|\   |
              B C D' G
              \     /
               E   /
                \ /
                 H
*/
            new TestCaseData(new[]
                {
                    "A",
                    "B > A",
                    "C > A",
                    "D > A *default",
                    "E > B",
                    "F",
                    "G > F",
                    "H > E, G"

                }, new[] { "A", "F", "B", "C", "D", "G", "E", "H" })
                .SetName("Complex tree"),
        };

        private ConfigurationHierarchy GetHierarchy(IEnumerable<string> configLines)
        {
            var lines = configLines.Select(line => parser.Parse(line)).ToArray();
            return new ConfigurationHierarchy(lines);
        }
    }
}
