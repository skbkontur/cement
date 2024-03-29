using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.CommandsTests;

[TestFixture]
internal class TestCheckDeps
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;

    public TestCheckDeps()
    {
        consoleWriter = ConsoleWriter.Shared;
        depsValidatorFactory = new DepsValidatorFactory();
    }

    [Test]
    public void TestCollectReferences()
    {
        using var tempDir = new TempDirectory();
        Helper.SetWorkspace(tempDir.Path);

        var cementModules = new List<string>
        {
            "Dep1",
            "Dep2"
        };

        var slnContent = @"
Project(""A"") = ""AName"", ""AName/AName.csproj"", ""{AAA}""
Project(""B"") = ""BName"", ""BName/BName.csproj"", ""{BBB}""
Project(""C"") = ""CName"", ""CName/CName.csproj"", ""{CCC}""

{AAA}.Client|Any CPU.Build.0 = Release|Any CPU
{BBB}.Release|Any CPU.Build.0 = Release|Any CPU
{CCC}.Client|Any CPU.Build.0 = Release|Any CPU

".ReplaceLineEndings();

        Directory.CreateDirectory(Path.Combine(tempDir.Path, "AName"));
        File.WriteAllText(
            Path.Combine(tempDir.Path, "AName", "AName.csproj"), @"
<root>
<HintPath>../Dep2/src/bin/Kontur.Logging.dll</HintPath>
<HintPath>../logging/src/bin/Kontur.Logging.dll</HintPath>
</root>".ReplaceLineEndings());

        Directory.CreateDirectory(Path.Combine(tempDir.Path, "BName"));
        File.WriteAllText(
            Path.Combine(tempDir.Path, "BName", "BName.csproj"), @"
<root>
<HintPath>../Dep1/src/bin/Kontur.Logging.dll</HintPath>
<HintPath>../logging/src/bin/Kontur.Logging.dll</HintPath>
</root>".ReplaceLineEndings());

        Directory.CreateDirectory(Path.Combine(tempDir.Path, "CName"));
        File.WriteAllText(
            Path.Combine(tempDir.Path, "CName", "CName.csproj"), @"
<root>
<HintPath>../Dep10/src/bin/Kontur.Logging.dll</HintPath>
<HintPath>../logging/src/bin/Kontur.Logging.dll</HintPath>
</root>".ReplaceLineEndings());

        File.WriteAllText(Path.Combine(tempDir.Path, "solution.sln"), slnContent);
        var vsParser = new VisualStudioProjectParser(Path.Combine(tempDir.Path, "solution.sln"), cementModules);
        var references = vsParser.GetReferences(new BuildData("solution.sln", "Client"));

        references.Should().BeEquivalentTo(Path.Combine("Dep2", "src", "bin", "Kontur.Logging.dll"));
    }

    [Test]
    public void TestDepsReferencesCollector()
    {
        using var tempDir = new TempDirectory();
        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "A"), @"
full-build:
  build:
    target: 1.sln
    configuration: None
  deps:
    - B
    - C");
        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "B"), @"
full-build:
  build:
    target: 1.sln
    configuration: None
  install:
    - bin/Release/B.dll");

        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "C"), @"
full-build:
  build:
    target: 1.sln
    configuration: None
  deps:
    B");
        Helper.SetWorkspace(tempDir.Path);
        var depsReferences = new DepsReferencesCollector(consoleWriter, depsValidatorFactory, Path.Combine(tempDir.Path, "A"), null)
            .GetRefsFromDeps();

        Assert.AreEqual(new[] {"C"}, depsReferences.NotFoundInstallSection);
        Assert.AreEqual(new[] {"B\\bin\\Release\\B.dll"}, depsReferences.FoundReferences.First().InstallFiles);
        Assert.AreEqual(new[] {"B\\bin\\Release\\B.dll"}, depsReferences.FoundReferences.First().CurrentConfigurationInstallFiles);
    }

    [Test]
    public void TestDepsReferencesCollectorMainConfig()
    {
        using var tempDir = new TempDirectory();
        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "A"), @"
full-build:
  build:
    target: 1.sln
    configuration: None

  deps:
    - B
    - C");
        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "B"), @"
client:
  build:
    target: 1.sln
    configuration: None

  install:
    - bin/Release/B.Client.dll

full-build > client:
  install:
    - bin/Release/B.dll");

        MakeDirectoryAndWriteYaml(
            Path.Combine(tempDir.Path, "C"), @"
full-build:
  build:
    target: 1.sln
    configuration: None

  deps:
    B");
        Helper.SetWorkspace(tempDir.Path);
        var depsReferences = new DepsReferencesCollector(consoleWriter, depsValidatorFactory, Path.Combine(tempDir.Path, "A"), null)
            .GetRefsFromDeps();

        Assert.AreEqual(new[] {"C"}, depsReferences.NotFoundInstallSection);
        Assert.AreEqual(new[] {"B\\bin\\Release\\B.dll", "B\\bin\\Release\\B.Client.dll"}, depsReferences.FoundReferences.First().InstallFiles);
        Assert.AreEqual(new[] {"B\\bin\\Release\\B.dll"}, depsReferences.FoundReferences.First().CurrentConfigurationInstallFiles);
    }

    private void MakeDirectoryAndWriteYaml(string path, string content)
    {
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, Helper.YamlSpecFile), content);
    }
}
