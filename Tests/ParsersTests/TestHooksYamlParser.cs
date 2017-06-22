using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestHooksYamlParser
    {
        [Test]
        public void HooksGet()
        {
            const string text = @"
default:
  hooks:
    - a
    - b/c

client:
";

            Assert.AreEqual(new[] {"a", "b/c"}, YamlFromText.HooksParser(text).Get());
        }

        [Test]
        public void HooksEmpty()
        {
            const string text = @"
default:
  hooks:

client:
";

            Assert.That(YamlFromText.HooksParser(text).Get().Count == 0);
        }

        [Test]
        public void NoHooksSection()
        {
            const string text = @"
default:

client:
";

            Assert.That(YamlFromText.HooksParser(text).Get().Count == 0);
        }

        [Test]
        public void NoDefaultSection()
        {
            const string text = @"
client:
";

            Assert.That(YamlFromText.HooksParser(text).Get().Count == 0);
        }
    }
}
