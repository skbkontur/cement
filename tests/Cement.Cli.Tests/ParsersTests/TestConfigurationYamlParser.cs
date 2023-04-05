using System.IO;
using System.Linq;
using Cement.Cli.Tests.Helpers;
using Cement.Cli.Common;
using Cement.Cli.Common.YamlParsers;
using NUnit.Framework;

namespace Cement.Cli.Tests.ParsersTests;

[TestFixture]
public class TestYamlParser
{
    [Test]
    public void TestGetConfigurations()
    {
        const string text = @"
default:
sdk:
client:";
        Assert.AreEqual(new[] {"sdk", "client"}.ToList(), YamlFromText.ConfigurationParser(text).GetConfigurations());
    }

    [Test]
    public void TestGetConfigurationsRealNames()
    {
        const string text = @"
default:
sdk > client:
client *default:";
        Assert.AreEqual(new[] {"sdk", "client"}.ToList(), YamlFromText.ConfigurationParser(text).GetConfigurations());
    }

    [Test]
    public void TestConfigExists()
    {
        const string text = @"
default:
sdk > client:
client *default:";
        Assert.IsTrue(YamlFromText.ConfigurationParser(text).ConfigurationExists("sdk"));
    }

    [Test]
    public void TestConfigNotExists()
    {
        const string text = @"
default:
sdk > client:
client *default:";
        Assert.IsFalse(YamlFromText.ConfigurationParser(text).ConfigurationExists("release"));
    }

    [Test]
    public void TestGetDefaultConfigName()
    {
        const string text = @"
default:
sdk > client:
client *default:";
        Assert.AreEqual("client", YamlFromText.ConfigurationParser(text).GetDefaultConfigurationName());
    }

    [Test]
    public void TestGetParentConfigurations()
    {
        const string text = @"
default:
full-build > sdk, client *default:
sdk > client:
client:";
        Assert.AreEqual(
            new[] {"sdk", "client"}.ToList(),
            YamlFromText.ConfigurationParser(text).GetParentConfigurations("full-build"));
    }

    [Test]
    public void TestGetParentConfigurationsIsNull()
    {
        const string text = @"
default:
full-build > sdk, client *default:
sdk > client:
client:";
        Assert.IsNull(YamlFromText.ConfigurationParser(text).GetParentConfigurations("client"));
    }

    [Test]
    public void TestGetConfigurationHierarchy()
    {
        const string text = @"
default:
full-build > sdk, client *default:
sdk > client:
client:
notests:";
        var hierarchy = YamlFromText.ConfigurationParser(text).GetConfigurationsHierarchy();
        Assert.NotNull(hierarchy);
        Assert.AreEqual(2, hierarchy["client"].Count);
        Assert.AreEqual(0, hierarchy["full-build"].Count);
        Assert.AreEqual(1, hierarchy["sdk"].Count);
        Assert.AreEqual(0, hierarchy["notests"].Count);
    }

    [Test]
    public void TestSampleContentFromAFile()
    {
        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            File.WriteAllText(
                "module.yaml", @"
default:
client:
sdk:
");
            var configurations = new ConfigurationYamlParser(new FileInfo(tempDir.Path)).GetConfigurations();
            Assert.AreEqual(2, configurations.Count);
        }
    }

    [Test]
    public void TestGetConfigurationDescription()
    {
        const string ymlWithEmptyConfig = @"
default:
client:
";
        var props = new ConfigurationYamlParser("test1", ymlWithEmptyConfig).GetConfigurationDescription("default");
        Assert.That(props, Is.Null);

        const string ymlWithoutProps = @"default:
  deps:
    - dep1
    - dep2
";
        props = new ConfigurationYamlParser("test2", ymlWithoutProps).GetConfigurationDescription("default");
        Assert.That(props, Has.Count.EqualTo(1));

        const string ymlWithProps = @"default:
  flag: true
  custom: ""text""
  deps:
    - dep1
    - dep2
";
        props = new ConfigurationYamlParser("test3", ymlWithProps).GetConfigurationDescription("default");
        Assert.That(props, Has.Count.EqualTo(3));
        Assert.That(props["flag"], Is.EqualTo(true));
        Assert.That(props["custom"], Is.EqualTo("text"));
    }
}
