using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    class TestInstallXmlParser
    {
        [Test]
        public void TestGetDefaultConfig()
        {
            var content = @"
<module>
	<install>
		<add-ref src=""A/B.dll"" />
		<add-ref src=""C/D.dll"" />
	</install>
</module>
";
            var result = new InstallXmlParser(content, "module").Get(null);
            Assert.AreEqual(2, result.CurrentConfigurationInstallFiles.Count);
            Assert.AreEqual(new[] {"A/B.dll", "C/D.dll"}, result.CurrentConfigurationInstallFiles.ToArray());
        }

        [Test]
        public void TestGetNotDefaultConfigWhenNoSuchConfig()
        {
            var content = @"
<module>
	<install>
		<add-ref src=""A/B.dll"" />
		<add-ref src=""C/D.dll"" />
	</install>
</module>
";
            Assert.Throws<NoSuchConfigurationException>(() => new InstallXmlParser(content, "module").Get("client"));
        }

        [Test]
        public void TestGetDefaultConfigWhenNoSuchConfig()
        {
            var content = @"
<spec>
	<module>
		<install target=""client"">
			<add-ref src=""Kontur.Drive.Client/bin/Release/Kontur.Drive.Client.dll""/>
			<add-ref src=""Kontur.Drive.ServiceModel/bin/Release/Kontur.Drive.ServiceModel.dll""/>
		</install>
		<install target=""local"">
			<add-ref src=""Kontur.Drive.TestHost/bin/Release/Kontur.Drive.TestHost.exe""/>
			<add-ref src=""Kontur.Drive.TestHost/bin/Release/ServiceStack.Interfaces.dll""/>
		</install>
	</module>
	<configurations>
		<conf name=""client"" parents=""sdk""/>
		<conf name=""sdk""/>
		<default-config name=""sdk""/>
	</configurations>
</spec>";
            Assert.Throws<NoSuchConfigurationException>(() => new InstallXmlParser(content, "module").Get(null));
        }

        [Test]
        public void TestGetCustomConfig()
        {
            var content = @"
<spec>
	<module>
		<install target=""client"">
			<add-ref src=""Kontur.Drive.Client/bin/Release/Kontur.Drive.Client.dll""/>
			<add-ref src=""Kontur.Drive.ServiceModel/bin/Release/Kontur.Drive.ServiceModel.dll""/>
		</install>
		<install target=""local"">
			<add-ref src=""Kontur.Drive.TestHost/bin/Release/Kontur.Drive.TestHost.exe""/>
			<add-ref src=""Kontur.Drive.TestHost/bin/Release/ServiceStack.Interfaces.dll""/>
		</install>
	</module>
	<configurations>
		<conf name=""client"" parents=""sdk""/>
		<conf name=""sdk""/>
		<default-config name=""sdk""/>
	</configurations>
</spec>";
            var result = new InstallXmlParser(content, "module").Get("client");
            Assert.AreEqual(new[]
            {
                "Kontur.Drive.Client/bin/Release/Kontur.Drive.Client.dll",
                "Kontur.Drive.ServiceModel/bin/Release/Kontur.Drive.ServiceModel.dll"
            }, result.CurrentConfigurationInstallFiles.ToArray());
        }
    }
}