using System.Collections.Generic;
using Cement.Cli.Common;
using Cement.Cli.Common.YamlParsers;
using Cement.Cli.Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;

namespace Cement.Cli.Tests.ParsersTests.V2;

[TestFixture]
public class TestDepsSectionParser
{
    private static TestCaseData[] NoParentDeps =
    {
        new TestCaseData(
                @"deps:",
                new DepsData(null, new List<Dep>()))
            .SetName("No deps, no force"),

        new TestCaseData(
                @"deps:
  - module.a",
                new DepsData(
                    null, new List<Dep>
                    {
                        new("module.a", null)
                    }))
            .SetName("No parent deps, no force, single single-line module"),

        new TestCaseData(
                @"deps:
  - module.a
  - module.b
  - module.c
",
                new DepsData(
                    null, new List<Dep>
                    {
                        new("module.a", null),
                        new("module.b", null),
                        new("module.c", null)
                    }))
            .SetName("No parent deps, no force, multiple single-line modules"),

        new TestCaseData(
                @"deps:
  - module.a:
    type: src
    treeish: somebranch
    configuration: client
  - module.b
  - module.c
",
                new DepsData(
                    null, new List<Dep>
                    {
                        new("module.a", "somebranch", "client"),
                        new("module.b", null),
                        new("module.c", null)
                    }))
            .SetName("No parent deps, no force, single multiple-line module and multiple single-line modules"),

        new TestCaseData(
                @"deps:
  - force: $CURRENT_BRANCH
  - module.a
  - module.b
  - module.c
",
                new DepsData(
                    new[] {"$CURRENT_BRANCH"}, new List<Dep>
                    {
                        new("module.a", null),
                        new("module.b", null),
                        new("module.c", null)
                    }))
            .SetName("No parent deps, one force, multiple single-line modules"),

        new TestCaseData(
                @"deps:
  - force: $CURRENT_BRANCH,branch_a, branch_b
  - module.a
  - module.b
  - module.c
",
                new DepsData(
                    new[] {"$CURRENT_BRANCH", "branch_a", "branch_b"}, new List<Dep>
                    {
                        new("module.a", null),
                        new("module.b", null),
                        new("module.c", null)
                    }))
            .SetName("No parent deps, multiple force, multiple single-line modules"),

        new TestCaseData(
                @"deps:
  - module.a
  - module.b
  - module.c
  - force: $CURRENT_BRANCH,branch_a, branch_b
",
                new DepsData(
                    new[] {"$CURRENT_BRANCH", "branch_a", "branch_b"}, new List<Dep>
                    {
                        new("module.a", null),
                        new("module.b", null),
                        new("module.c", null)
                    }))
            .SetName("No parent deps, multiple force in the end, multiple single-line modules"),

        new TestCaseData(
                @"deps:
  - module.a
  - -module.a
  - module.c
",
                new DepsData(
                    null, new List<Dep>
                    {
                        new("module.c", null)
                    }))
            .SetName("No parent deps, no force, removing module within current deps section"),

        new TestCaseData(
                @"deps:
  - module.a
  - -module.a
  - module.a
  - -module.a
  - module.c
",
                new DepsData(
                    null, new List<Dep>
                    {
                        new("module.c", null)
                    }))
            .SetName("No parent deps, no force, removing module within current deps section")
    };

    [TestCaseSource(nameof(NoParentDeps))]
    public void ParseDepsSection(string input, DepsData expected)
    {
        var yaml = GetDepsSections(input);

        var parser = new DepsSectionParser(new DepSectionItemParser());
        var merger = new DepsSectionMerger();

        var parsed = parser.Parse(yaml);
        var merged = merger.Merge(parsed);

        merged.Should().BeEquivalentTo(expected);
    }

    private object GetDepsSections(string text)
    {
        var serializer = new Serializer();
        var yaml = (Dictionary<object, object>)serializer.Deserialize(text);

        return yaml["deps"];
    }
}
