using System.Collections.Generic;
using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
	[TestFixture]
	public class TestConfigurationXmlPasrer
	{
		[Test]
		public void TestGetConfigurationsNames()
		{
			var text = @"
<configurations>
	<conf name=""client"" parents=""sdk""/>
	<conf name=""sdk""/>
	<default-config name=""sdk""/> 
</configurations>";
			var configurations = new ConfigurationXmlParser(text).GetConfigurations();
			Assert.AreEqual(new List<string> { "client", "sdk" }, configurations);
		}

		[Test]
		public void TestGetDefaultConfiguration()
		{
			var text = @"
<configurations>
	<conf name=""client"" parents=""sdk""/>
	<conf name=""sdk""/>
	<default-config name=""sdk""/> 
</configurations>";
			var defaultConfig = new ConfigurationXmlParser(text).GetDefaultConfigurationName();
			Assert.AreEqual("sdk", defaultConfig);
		}

		[Test]
		public void TestGetConfigurationParents()
		{
			var text = @"
<configurations>
	<conf name=""client"" parents=""sdk, full-build""/>
	<conf name=""sdk""/>
	<default-config name=""sdk""/> 
</configurations>";
			var parents = new ConfigurationXmlParser(text).GetParentConfigurations("client");
			Assert.AreEqual(new[] { "sdk", "full-build" }, parents);
		}

		[Test]
		public void TestContainsConfigTrue()
		{
			var text = @"
<configurations>
	<conf name=""client"" parents=""sdk, full-build""/>
	<conf name=""sdk""/>
	<default-config name=""sdk""/> 
</configurations>";
			Assert.True(new ConfigurationXmlParser(text).ConfigurationExists("sdk"));
		}

		[Test]
		public void TestContainsConfigFalse()
		{
			var text = @"
<configurations>
	<conf name=""client"" parents=""sdk, full-build""/>
	<conf name=""sdk""/>
	<default-config name=""sdk""/> 
</configurations>";
			Assert.False(new ConfigurationXmlParser(text).ConfigurationExists("dev"));
		}


		[Test]
		public void TestGetConfigurationHierarchy()
		{
			const string text = @"
<configurations>
	<conf name=""client"" parents=""sdk, full-build""/>
	<conf name=""sdk"" parents = ""full-build""/>
    <conf name=""notests""/>
	<default-config name=""full-build""/> 
</configurations>";
			var hierarchy = new ConfigurationXmlParser(text).GetConfigurationsHierarchy();
			Assert.NotNull(hierarchy);
			Assert.AreEqual(2, hierarchy["client"].Count);
			Assert.AreEqual(0, hierarchy["full-build"].Count);
			Assert.AreEqual(1, hierarchy["sdk"].Count);
			Assert.AreEqual(1, hierarchy["notests"].Count);
		}

	}
}
