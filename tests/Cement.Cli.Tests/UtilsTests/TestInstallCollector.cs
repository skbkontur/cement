using System.IO;
using Cement.Cli.Common;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestInstallCollector
{
    [Test]
    public void TestWithExternals()
    {
        var externalModuleText = @"

full-build:
  install:
    - external
    - nuget pExternal
".ReplaceLineEndings();

        var moduleText = @"

full-build:
  deps:
    - ext
  install:
    - current
    - module ext
    - nuget pCurrent".ReplaceLineEndings();

        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            CreateModule("ext", externalModuleText);
            CreateModule("cur", moduleText);
            var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
            var installFiles = installData.InstallFiles!.ToArray();
            var nugetPackages = installData.NuGetPackages!.ToArray();

            installFiles.Should().BeEquivalentTo(Path.Combine("cur", "current"), Path.Combine("ext", "external"));
            nugetPackages.Should().BeEquivalentTo("pCurrent", "pExternal");
        }
    }

    [Test]
    public void TestCollectInstallWithExternalClientConfig()
    {
        var externalModuleText = @"

full-build:
  install:
    - external
client:
  install:
    - external.client
".ReplaceLineEndings();

        var moduleText = @"

full-build:
  deps:
    - ext
  install:
    - current
    - module ext/client
".ReplaceLineEndings();

        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            CreateModule("ext", externalModuleText);
            CreateModule("cur", moduleText);
            var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();

            result.InstallFiles.Should().BeEquivalentTo(Path.Combine("cur", "current"), Path.Combine("ext", "external.client"));
        }
    }

    [Test]
    public void TestLongNestingsWithConfigs()
    {
        var qText = @"

full-build:
  install:
sdk:
  install:
    - q.sdk
    - module ext/client
".ReplaceLineEndings();

        var externalModuleText = @"

full-build:
  install:
    - external
    - module q/sdk
client:
  install:
    - external.client
".ReplaceLineEndings();

        var moduleText = @"

full-build:
  deps:
    - ext
  install:
    - current
    - module ext
".ReplaceLineEndings();

        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            CreateModule("q", qText);
            CreateModule("ext", externalModuleText);
            CreateModule("cur", moduleText);
            var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();

            result.InstallFiles.Should().BeEquivalentTo(
                Path.Combine("cur", "current"),
                Path.Combine("ext", "external"),
                Path.Combine("q", "q.sdk"),
                Path.Combine("ext", "external.client"));
        }
    }

    [Test]
    public void TestLongNestingsWithConfigsNexting()
    {
        var qText = @"

full-build:
  install:
sdk:
  install:
    - q.sdk
    - module ext/client
    - nuget q.sdk
".ReplaceLineEndings();

        var externalModuleText = @"

full-build:
  install:
    - external
    - module q/sdk
    - nuget external
client:
  install:
    - external.client
    - nuget external.client
".ReplaceLineEndings();

        var moduleText = @"

full-build > client:
  deps:
    - ext
  install:
    - current
    - module ext
    - nuget current
client:
  install:
    - current.client
    - module ext/client
    - nuget client
".ReplaceLineEndings();

        using var tempDir = new TempDirectory();
        using (new DirectoryJumper(tempDir.Path))
        {
            CreateModule("q", qText);
            CreateModule("ext", externalModuleText);
            CreateModule("cur", moduleText);
            var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();

            installData.InstallFiles.Should().BeEquivalentTo(
                Path.Combine("cur", "current"),
                Path.Combine("cur", "current.client"),
                Path.Combine("ext", "external"),
                Path.Combine("ext", "external.client"),
                Path.Combine("q", "q.sdk"));
            installData.NuGetPackages.Should().BeEquivalentTo(
                "current", "client", "external", "external.client", "q.sdk");
        }
    }

    private void CreateModule(string moduleName, string content)
    {
        if (!Directory.Exists(moduleName))
        {
            Directory.CreateDirectory(moduleName);
        }

        var filePath = Path.Combine(moduleName, "module.yaml");
        File.WriteAllText(filePath, content);
    }
}
