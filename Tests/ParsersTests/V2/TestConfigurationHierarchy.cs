using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.YamlParsers.V2;
using Common.YamlParsers.V2.Factories;
using NUnit.Framework;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestConfigurationHierarchyFactory
    {
        private readonly ConfigSectionTitleParser parser;

        public TestConfigurationHierarchyFactory()
        {
            this.parser = new ConfigSectionTitleParser();
        }

        [TestCaseSource(nameof(FindAllParentSource))]
        public void FindAllParents(string childConfig, string[] configLines, string[] expectedParents)
        {
            var hierarchy = GetHierarchy(configLines);
            var actualParents = hierarchy.GetAllParents(childConfig);
            var msg = "Actual: " + string.Join(", ", actualParents)
                                 + Environment.NewLine
                                 + "Expected: " + string.Join(", ", expectedParents);
            Assert.AreEqual(expectedParents, actualParents, msg);
        }

        [TestCaseSource(nameof(FindDefaultSource))]
        public string FindDefault(List<string> configLines)
        {
            var hierarchy = GetHierarchy(configLines);
            return hierarchy.FindDefault();
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

        [Test, Timeout(1000)]
        public void ThrowOnCyclicDependencies()
        {
            var configLines = new[]
            {
                "A > C *default",
                "B > A",
                "C > B",
            };

            var lines = configLines.Select(line => parser.Parse(line)).ToArray();
            Assert.Throws<BadYamlException>(() => ConfigurationHierarchyFactory.Get(lines));
        }

        private static TestCaseData[] FindAllParentSource =
        {
            new TestCaseData("A", new[] { "A" }, new string[0])
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
            }, new[] { "A", "B" })
                .SetName("One root, 3 children, one grandchild"),

            new TestCaseData("E", new[]
                {
                    "A",
                    "B > A",
                    "C > A",
                    "D > A *default",
                    "E > B",
                    "F",
                    "G > F"
                }, new[] { "A", "B" })
                .SetName("Two independent roots, 3 children, one grandchild"),

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

            }, new[] { "A", "F", "B", "G", "E" })
                .SetName("Complex tree"),

            /*
                      A'
                    ／  ＼
                  B       C
                 /\＼    / \
                / |\ ＼／  /
               /  | \／＼ /
              H   G  D   E
              |   |  |   |
               \  I  F  /
                \ \ / ／
                 ＼|／
                   J
*/
            new TestCaseData("F", new[]
            {
                "A *default",
                "B > A",
                "C > A",
                "D > B,C",
                "E > B,C",
                "F > D",
                "G > B",
                "H > B",
                "I > G",
                "J > I,F,H,E",

            }, new[] { "A", "B", "C", "D" })
                .SetName("Complex tree with cross-inheritence, finding parents of a node in the middle"),

            new TestCaseData("J", new[]
            {
                "A *default",
                "B > A",
                "C > A",
                "D > B,C",
                "E > B,C",
                "F > D",
                "G > B",
                "H > B",
                "I > G",
                "J > I,F,H,E",

            }, new[] { "A", "B", "C", "G", "H", "D", "E", "I", "F" })
            .SetName("Complex tree with cross-inheritence, finding parents of the biggest node")
        };

        private static TestCaseData[] FindDefaultSource =
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
                    "A",
                    "B",
                })
                .Returns(null)
                .SetName("Two non-full-build configs, no default mark. Null default config"),

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

        };

        private static TestCaseData[] GetAllSource =
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
                    "client *default",
                    "full-build > non-existent-config"
                }, new[] { "client", "full-build" })
                .SetName("Two roots and non-existent config"),

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
              A'
             /|
            B |
             \|
              C
 */
            new TestCaseData(new[]
                {
                    "A *default",
                    "B > A",
                    "C > B,A",
                }, new[] { "A", "B", "C" })
                .SetName("A; B>A; C>B,A"),

/*
              A'
             / \
            B   C
            \   |
             \  D
              \/
               E
 */
            new TestCaseData(new[]
                {
                    "A *default",
                    "B > A",
                    "C > A",
                    "D > C",
                    "E > B,D",
                }, new[] { "A", "B", "C", "D", "E" })
                .SetName("Uneven paths' lengths to root"),

/*
              A'
             ／＼
            B   C
            |＼／|
            |／＼|
            D   E
             ＼／
              F
 */
            new TestCaseData(new[]
                {
                    "A *default",
                    "B > A",
                    "C > A",
                    "D > B,C",
                    "E > B,C",
                    "F > D,E",
                }, new[] { "A", "B", "C", "D", "E", "F" })
                .SetName("Cross-references"),

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
                .SetName("Complex tree without cross-inheritance"),

/*

                      A'
                    ／  ＼
                  B       C
                 /\＼    / \
                / |\ ＼／  /
               /  | \／＼ /
              H   G  D   E
              |   |  |   |
               \  I  F  /
                \ \ / ／
                 ＼|／
                   J
*/
            new TestCaseData(new[]
                {
                    "A *default",
                    "B > A",
                    "C > A",
                    "D > B,C",
                    "E > B,C",
                    "F > D",
                    "G > B",
                    "H > B",
                    "I > G",
                    "J > I,F,H,E",

                }, new[] { "A", "B", "C", "G", "H", "D", "E", "I", "F", "J" })
                .SetName("Complex tree with cross-inheritance"),
        };

        private ConfigurationHierarchy GetHierarchy(IEnumerable<string> configLines)
        {
            var lines = configLines.Select(line => parser.Parse(line)).ToArray();
            return ConfigurationHierarchyFactory.Get(lines);
        }
    }
}
