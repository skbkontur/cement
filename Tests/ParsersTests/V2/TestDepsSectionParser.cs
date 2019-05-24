// using System.Collections.Generic;
// using Common;
// using Common.YamlParsers;
// using Common.YamlParsers.V2;
// using FluentAssertions;
// using NUnit.Framework;
// using SharpYaml.Serialization;
//
// namespace Tests.ParsersTests.V2
// {
//     [TestFixture]
//     public class TestDepsSectionParser
//     {
//         [TestCaseSource(nameof(NoParentDeps))]
//         [TestCaseSource(nameof(WithParentDeps))]
//         public void ParseDepsSection(string input, DepsContent expectedResult)
//         {
//             var yaml = GetDepsSections(input);
//             var parser = new DepsSectionParser(new DepLineParser());
//
//             var actual = parser.Parse(yaml, null, parentDeps);
//
//             actual.Should().BeEquivalentTo(expectedResult);
//         }
//
//         [TestCaseSource(nameof(InvalidDeps))]
//         public void ThrowOnInvalidDepSection(string input, Dep[] parentDeps)
//         {
//             var yaml = GetDepsSections(input);
//             var parser = new DepsSectionParser(new DepLineParser());
//             parser.Parse(yaml, null, parentDeps);
//         }
//
//         private static TestCaseData[] NoParentDeps =
//         {
//             new TestCaseData(@"deps:",
//                     null,
//                     new DepsContent(null, new List<Dep>()))
//                 .SetName("No deps, no force"),
//
//             new TestCaseData(@"deps:
//   - module.a",
//                     null,
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null)
//                     }))
//                 .SetName("No parent deps, no force, single single-line module"),
//
//
//             new TestCaseData(@"deps:
//   - module.a
//   - module.b
//   - module.c
// ",
//                     null,
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, no force, multiple single-line modules"),
//
//             new TestCaseData(@"deps:
//   - module.a:
//     type: src
//     treeish: somebranch
//     configuration: client
//   - module.b
//   - module.c
// ",
//                     null,
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", "somebranch", "client") { NeedSrc = true },
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, no force, single multiple-line module and multiple single-line modules"),
//
//             new TestCaseData(@"deps:
//   - force: $CURRENT_BRANCH
//   - module.a
//   - module.b
//   - module.c
// ",
//                     null,
//                     new DepsContent(new [] {"$CURRENT_BRANCH"}, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, one force, multiple single-line modules"),
//
//             new TestCaseData(@"deps:
//   - force: $CURRENT_BRANCH,branch_a, branch_b
//   - module.a
//   - module.b
//   - module.c
// ",
//                     null,
//                     new DepsContent(new [] {"$CURRENT_BRANCH", "branch_a", "branch_b"}, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, multiple force, multiple single-line modules"),
//
//             new TestCaseData(@"deps:
//   - module.a
//   - module.b
//   - module.c
//   - force: $CURRENT_BRANCH,branch_a, branch_b
// ",
//                     null,
//                     new DepsContent(new [] {"$CURRENT_BRANCH", "branch_a", "branch_b"}, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, multiple force in the end, multiple single-line modules"),
//
//             new TestCaseData(@"deps:
//   - module.a
//   - -module.a
//   - module.c
// ",
//                     null,
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, no force, removing module within current deps section"),
//
//             new TestCaseData(@"deps:
//   - module.a
//   - -module.a
//   - module.a
//   - -module.a
//   - module.c
// ",
//                     null,
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("No parent deps, no force, removing module trwing within current deps section"),
//
//
//         };
//
//         private static TestCaseData[] WithParentDeps =
//         {
//             new TestCaseData(@"deps:
//   - module.c",
//                     new[]
//                     {
//                         new DepDep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     },
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                         new Dep("module.c", null, null)
//                     }))
//                 .SetName("Two parent deps, no force, single single-line module"),
//
//             new TestCaseData(@"deps:
//   - -module.a",
//                     new[]
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     },
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.b", null, null),
//                     }))
//                 .SetName("Two parent deps, child config deletes parent dep"),
//
//             new TestCaseData(@"deps:
//   - -module.a
//   - module.a@branch/conf
// ",
//                     new[]
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     },
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", "branch", "conf"),
//                         new Dep("module.b", null, null),
//                     }))
//                 .SetName("Two parent deps, child config customizes parent dep (single-line)"),
//
//             new TestCaseData(@"deps:
//   - -module.a
//   - module.a:
//     treeish: branch
//     configuration: conf
// ",
//                     new[]
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     },
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", "branch", "conf"),
//                         new Dep("module.b", null, null),
//                     }))
//                 .SetName("Two parent deps, child config customizes parent dep (multi-line)"),
//
//             new TestCaseData(@"deps:",
//                     new[]
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     },
//                     new DepsContent(null, new List<Dep>()
//                     {
//                         new Dep("module.a", null, null),
//                         new Dep("module.b", null, null),
//                     }))
//                 .SetName("Two parent deps, child config doesn't change any deps"),
//         };
//
//         private static TestCaseData[] InvalidDeps =
//         {
//             new TestCaseData(@"deps:
//   - module.a
//   - module.a
// ",
//                     null)
//                 .Throws(typeof(BadYamlException))
//                 .SetName("Cannot add same dep twice"),
//
//             new TestCaseData(@"deps:
//   - -module.a
// ",
//                     null)
//                 .Throws(typeof(BadYamlException))
//                 .SetName("Cannot remove not existing dep"),
//
//             new TestCaseData(@"deps:",
//                     new []
//                     {
//                         new Dep("module.a", null, "client"),
//                         new Dep("module.a", null, "full-build")
//                     })
//                 .Throws(typeof(BadYamlException))
//                 .SetName("One deps from parents collide (name-wise)"),
//
//             new TestCaseData(@"deps:",
//                     new []
//                     {
//                         new Dep("module.a", null, "client"),
//                         new Dep("module.a", null, "full-build"),
//                         new Dep("module.b", "branch-a"),
//                         new Dep("module.b", "branch-b")
//                     })
//                 .Throws(typeof(BadYamlException))
//                 .SetName("Two deps from parents collide (name-wise)"),
//
//             new TestCaseData(@"deps:
//   - module.a/full-build
// ",
//                     new []
//                     {
//                         new Dep("module.a", null, "client"),
//                     })
//                 .Throws(typeof(BadYamlException))
//                 .SetName("Cannot add dependency that was added in parent config"),
//         };
//
//         private object GetDepsSections(string text)
//         {
//             var serializer = new Serializer();
//             var yaml = (Dictionary<object, object>) serializer.Deserialize(text);
//
//             return yaml["deps"];
//         }
//     }
// }