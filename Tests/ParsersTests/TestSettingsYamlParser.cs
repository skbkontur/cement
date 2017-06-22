using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestSettingsYamlParser
    {
        [Test]
        public void SettingsTypeContent()
        {
            const string text = @"
default:
  settings:
    type: content";

            Assert.IsTrue(YamlFromText.SettingsParser(text).Get().IsContentModule);
        }

        [Test]
        public void SettingsTypeNoContent()
        {
            const string text = @"
default:
  settings:
    type: asdf";

            Assert.IsFalse(YamlFromText.SettingsParser(text).Get().IsContentModule);
        }

        [Test]
        public void SettingsNoSection()
        {
            const string text = @"
default:";

            Assert.IsFalse(YamlFromText.SettingsParser(text).Get().IsContentModule);
        }
    }
}