using System.Collections.Generic;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;
using Tests.Helpers;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestHooksSectionParser
    {
        [TestCaseSource(nameof(Source))]
        public void TestParse(string input, string[] expectedResult)
        {
            var parser = new HooksSectionParser();
            var hooks = GetHooksSections(input);

            var actual = parser.Parse(hooks);

            actual.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        }

        [TestCaseSource(nameof(Source))]
        public void TestParse2(string input, string[] expectedResult)
        {
            var actual = YamlFromText.HooksParser(input).Get();

            actual.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] Source =
        {
            new TestCaseData(@"
default:
  hooks:
",
                    new string[0])
                .SetName("Empty hooks section"),

            new TestCaseData(@"
default:
  hooks:
    - a
    - b/c
",
                new[] { "a", "b/c" })
                .SetName("Two hooks in defaults section"),

        };

        private object GetHooksSections(string text)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>) serializer.Deserialize(text);

            var defaultSection = yaml["default"] as Dictionary<object, object>;

            object hooks = null;
            defaultSection?.TryGetValue("hooks", out hooks);
            return hooks;
        }
    }
}