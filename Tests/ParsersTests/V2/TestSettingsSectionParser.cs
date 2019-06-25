using System.Collections.Generic;
using Common.YamlParsers;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestSettingsSectionParser
    {
        [TestCaseSource(nameof(Source))]
        public void TestParse(string input, ModuleSettings expectedResult)
        {
            var parser = new SettingsSectionParser();
            var settings = GetSettingsSection(input);

            var actual = parser.Parse(settings);

            actual.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] Source =
        {
            new TestCaseData(@"
default:
",
                    new ModuleSettings())
                .SetName("No settings section"),

            new TestCaseData(@"
default:
  settings:
",
                    new ModuleSettings())
                .SetName("Empty settings section"),

            new TestCaseData(@"
default:
  settings:
    somekey: somevalue
",
                    new ModuleSettings())
                .SetName("Settings section with unknown key-value"),

            new TestCaseData(@"
default:
  settings:
    type: content
",
                    new ModuleSettings() {IsContentModule = true})
                .SetName("Settings section with 'type: content'"),
        };

        private object GetSettingsSection(string text)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>) serializer.Deserialize(text);

            var defaultSection = yaml["default"] as Dictionary<object, object>;

            object hooks = null;
            defaultSection?.TryGetValue("settings", out hooks);
            return hooks;
        }
    }
}