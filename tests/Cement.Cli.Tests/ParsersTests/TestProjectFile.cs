using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Cement.Cli.Common;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.ParsersTests;

[TestFixture]
internal class TestProjectFile
{
    private TempDirectory workDirectory = new();

    [SetUp]
    public void SetUp()
    {
        workDirectory = new TempDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        workDirectory.Dispose();
    }

    [Test]
    public void TestCreatingDocument()
    {
        const string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
  <ItemGroup>
    <Reference Include=""log4net, Version=1.2.10.0, Culture=neutral, PublicKeyToken=1b44e1d426115821, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\log4net\log4net.dll</HintPath>
    </Reference>
    <Reference Include=""nunit.framework, Version=2.5.5.10112, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\nunit\nunit.framework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.XML"" />
  </ItemGroup>
</Project>
";
        var proj = CreateProjectFile(content);

        Assert.Multiple(() =>
        {
            Assert.That(proj.ContainsRef("log4net", out _), Is.True);
            Assert.That(proj.ContainsRef("logging", out _), Is.False);
        });
    }

    [Test]
    public void TestAddReference()
    {
        const string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
  <ItemGroup>
    <Reference Include=""log4net, Version=1.2.10.0, Culture=neutral, PublicKeyToken=1b44e1d426115821, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\log4net\log4net.dll</HintPath>
    </Reference>
    <Reference Include=""nunit.framework, Version=2.5.5.10112, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\nunit\nunit.framework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.XML"" />
  </ItemGroup>
</Project>
";
        var proj = CreateProjectFile(content);

        proj.AddRef("logging", "abc/def");

        Assert.Multiple(() =>
        {
            Assert.That(proj.ContainsRef("logging", out var refXml), Is.True);
            Assert.That(refXml.LastChild!.InnerText, Is.EqualTo("abc/def"));
        });
    }

    [Test]
    public void TestAddReferenceInEmptyProject()
    {
        const string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
</Project>
";
        var proj = CreateProjectFile(content);

        proj.AddRef("logging", "abc/def");

        Assert.Multiple(() =>
        {
            Assert.That(proj.ContainsRef("logging", out var refXml), Is.True);
            Assert.That(refXml.LastChild!.InnerText, Is.EqualTo("abc/def"));
        });
    }

    [Test]
    public void TestAddReferenceReferenceNotFirst()
    {
        const string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
  <ItemGroup />
  <ItemGroup>
    <Shit />
    <Reference Include=""log4net, Version=1.2.10.0, Culture=neutral, PublicKeyToken=1b44e1d426115821, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\log4net\log4net.dll</HintPath>
    </Reference>
    <Reference Include=""nunit.framework, Version=2.5.5.10112, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\nunit\nunit.framework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.XML"" />
  </ItemGroup>
</Project>
";
        var proj = CreateProjectFile(content);

        proj.AddRef("logging", "abc/def");

        Assert.Multiple(() =>
        {
            Assert.That(proj.ContainsRef("logging", out var refXml), Is.True);
            Assert.That(refXml.LastChild!.InnerText, Is.EqualTo("abc/def"));
        });
    }

    [Test]
    public void TestReplaceReferencePath()
    {
        const string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
  <ItemGroup>
    <Reference Include=""log4net, Version=1.2.10.0, Culture=neutral, PublicKeyToken=1b44e1d426115821, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\log4net\log4net.dll</HintPath>
    </Reference>
    <Reference Include=""nunit.framework, Version=2.5.5.10112, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\nunit\nunit.framework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.XML"" />
  </ItemGroup>
</Project>
";
        var proj = CreateProjectFile(content);

        proj.ReplaceRef("log4net", "abc/def");

        Assert.Multiple(() =>
        {
            Assert.That(proj.ContainsRef("log4net", out var refXml), Is.True);
            Assert.That(refXml.LastChild!.InnerText, Is.EqualTo("abc/def"));
        });
    }

    [TestCase(DefaultOldCsprojXml, TestName = "OldCsprojFormat")]
    [TestCase(DefaultNewCsprojXml, TestName = "NewCsprojFormat")]
    public void Constructor_GetXmlFromFile_IfFileExist(string csprojXml)
    {
        var projectFile = CreateProjectFile(csprojXml);

        Assert.That(projectFile.Document.OuterXml, Is.EqualTo(WithoutXmlFormatting(csprojXml)));
    }

    [Test]
    public void Constructor_ThrowException_IfFileNotExist()
    {
        var nullName = Guid.NewGuid().ToString();
        var nullPath = Path.Combine(workDirectory.Path, $"{nullName}.csproj");
        if (File.Exists(nullPath))
            File.Delete(nullPath);

        Assert.Throws<FileNotFoundException>(() => new ProjectFile(nullPath));
    }

    [TestCase(DefaultOldCsprojXml, TestName = "OldCsprojFormat")]
    [TestCase(DefaultNewCsprojXml, TestName = "NewCsprojFormat")]
    public void BindRuleset_NoBindRuleset_IfBindingAlreadyExists(string csprojXml)
    {
        const string rulesetName = "Other.ruleset";
        var rulesetPath = Path.Combine(workDirectory.Path, rulesetName);
        var rulesetFile = new RulesetFile(rulesetPath);
        var projectFile = CreateProjectFile(csprojXml);

        projectFile.BindRuleset(rulesetFile);

        Console.WriteLine("ProjectFile should be contains 2 old rulesetBindings:");
        Console.WriteLine(projectFile.Document.OuterXml);
        var rulesetBindings = SearchByXpath(projectFile.Document, "//a:CodeAnalysisRuleSet");
        Assert.That(rulesetBindings, Has.Count.EqualTo(2));
    }

    [TestCase(DefaultOldCsprojXml, TestName = "OldCsprojFormat")]
    [TestCase(DefaultNewCsprojXml, TestName = "NewCsprojFormat")]
    public void BindRuleset_MoveOldRulesetBindings_FromProjectFileToRulesetFile(string csprojXml)
    {
        var rulesetName = $"{Guid.NewGuid()}.ruleset";
        var rulesetPath = Path.Combine(workDirectory.Path, rulesetName);
        var rulesetFile = new RulesetFile(rulesetPath);
        var projectFile = CreateProjectFile(csprojXml);

        projectFile.BindRuleset(rulesetFile);

        Console.WriteLine($"ProjectFile should be contains only one CodeAnalysisRuleSet with value '{rulesetName}':");
        Console.WriteLine(projectFile.Document.OuterXml);
        var rulesetBinding = SearchByXpath(projectFile.Document, "//a:CodeAnalysisRuleSet").Single();
        Assert.That(rulesetBinding.InnerText, Is.EqualTo(rulesetName));

        Console.WriteLine("RulesetFile should be contains oldRulesetBindings:");
        Console.WriteLine(rulesetFile.Document.OuterXml);
        var oldRulesetBindings = SearchByXpath(rulesetFile.Document, "//a:Include");
        Assert.That(oldRulesetBindings, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddAnalyzer_CreateItemGroupForAnalyzers_IfNotExists_ForOldCsprojFormat()
    {
        var analyzerDllPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.dll");
        const string csprojContent = @"<?xml version=""1.0"" encoding=""utf-8""?><Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""></Project>";
        var projectFile = CreateProjectFile(csprojContent);

        projectFile.AddAnalyzer(analyzerDllPath);

        Assert.That(SearchByXpath(projectFile.Document, "//a:ItemGroup[a:Analyzer]").Single(), Is.Not.Null);
    }

    [Test]
    public void AddAnalyzer_CreateItemGroupForAnalyzers_IfNotExists_ForNewCsprojFormat()
    {
        var analyzerDllPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.dll");
        const string csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>";
        var projectFile = CreateProjectFile(csprojContent);

        projectFile.AddAnalyzer(analyzerDllPath);

        Assert.That(SearchByXpath(projectFile.Document, "//a:ItemGroup[a:Analyzer]").Single(), Is.Not.Null);
    }

    [TestCase(DefaultOldCsprojXml, TestName = "OldCsprojFormat")]
    [TestCase(DefaultNewCsprojXml, TestName = "NewCsprojFormat")]
    public void AddAnalyzer_AddAnalyzersDll_IfNotExists(string csprojXml)
    {
        var analyzerDllRelpath = $"{Guid.NewGuid()}.dll";
        var analyzerDllFullPath = Path.Combine(workDirectory.Path, analyzerDllRelpath);
        var projectFile = CreateProjectFile(csprojXml);

        projectFile.AddAnalyzer(analyzerDllFullPath);

        Assert.NotNull(SearchByXpath(projectFile.Document, $"//a:ItemGroup/a:Analyzer[@Include = '{analyzerDllRelpath}']").Single());
    }

    [TestCase(DefaultOldCsprojXml, TestName = "OldCsprojFormat")]
    [TestCase(DefaultNewCsprojXml, TestName = "NewCsprojFormat")]
    public void AddAnalyzer_NotAddAnalyzersDll_IfDllWithSomeNameAlreadyAdded(string csprojXml)
    {
        const string analyzerDllFileName = "Another.dll";
        var analyzerDllFullPath = Path.Combine(workDirectory.Path, analyzerDllFileName);
        var projectFile = CreateProjectFile(csprojXml);

        projectFile.AddAnalyzer(analyzerDllFullPath);

        SearchByXpath(projectFile.Document, "//a:ItemGroup/a:Analyzer[@Include = 'Another.dll']").Should().BeEmpty();
        SearchByXpath(projectFile.Document, "//a:ItemGroup/a:Analyzer[@Include = 'dummyDir/Another.dll']")
            .Should().ContainSingle();
    }

    [Test]
    public void TestMakeCsProjWithNugetReferences()
    {
        const string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>Vostok.Clusterclient</RootNamespace>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Authors>Vostok team</Authors>
    <Company>SKB Kontur</Company>
    <Product>Vostok</Product>
    <Description>ClusterClient library enables developers to build resilient and efficient clients for HTTP microservices.</Description>
    <PackageProjectUrl />
    <RepositoryUrl>https://github.com/vostok-project/clusterclient</RepositoryUrl>
    <PackageTags>vostok clusterclient</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include = ""Vostok.Core"" >
        <HintPath>..\..\vostok.core\Vostok.Core\bin\Release\netstandard2.0\Vostok.Core.dll </HintPath>
    </Reference>
  </ItemGroup>
</Project>
";

        var proj = CreateProjectFile(content);
        var xmlDocument = proj.CreateCsProjWithNugetReferences(new List<Dep> {new("vostok.core")}, true);
        Assert.Multiple(() =>
        {
            Assert.That(xmlDocument.SelectSingleNode("//Reference[@Include='Vostok.Core']"), Is.Null);
            Assert.That(xmlDocument.SelectSingleNode("//PackageReference[@Include='vostok.core']"), Is.Not.Null);
        });
    }

    private ProjectFile CreateProjectFile(string projectFileContent)
    {
        var projectFilePath = CreateProjectFilePath();
        File.WriteAllText(projectFilePath, projectFileContent);
        var projectFile = new ProjectFile(projectFilePath);
        return projectFile;
    }

    private string CreateProjectFilePath()
    {
        var currentTestName = TestContext.CurrentContext.Test.Name;
        foreach (var badSymbols in Path.GetInvalidFileNameChars())
            currentTestName = currentTestName.Replace(badSymbols, '_');
        var projectFileName = $"{currentTestName}.csproj";

        var projectFilePath = Path.Combine(workDirectory.Path, projectFileName);
        return projectFilePath;
    }

    private static string WithoutXmlFormatting(string xml) => new Regex(">\\s+<").Replace(xml, "><");

    private static List<XmlNode> SearchByXpath(XmlDocument xmlDocument, string xpath)
    {
        var namespaceUri = xmlDocument.DocumentElement!.NamespaceURI;
        var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
        nsmgr.AddNamespace("a", namespaceUri);
        var result = xmlDocument
            .SelectNodes(xpath, nsmgr)!
            .Cast<XmlNode>()
            .ToList();
        return result;
    }

    private const string DefaultOldCsprojXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>TestProject</RootNamespace>
    <AssemblyName>TestProject</AssemblyName>
    <CodeAnalysisRuleSet>Other.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PropertyGroupWithCondition>true</PropertyGroupWithCondition>
    <CodeAnalysisRuleSet>Another.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""LalalaReference"">
      <HintPath>../../lalala/LalalaReference.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include=""Other.dll"" />
    <Analyzer Include=""dummyDir/Another.dll"" />
  </ItemGroup>
</Project>";

    private const string DefaultNewCsprojXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <NoWarn>1701;1702;1591;1573</NoWarn>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <CodeAnalysisRuleSet>Other.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <PropertyGroup Condition=""condition"">
    <NoWarn>1</NoWarn>
    <CodeAnalysisRuleSet>Another.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""LalalaReference"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\..\lalalal\bin\LalalaReference.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include=""Other.dll"" />
    <Analyzer Include=""dummyDir/Another.dll"" />
  </ItemGroup>
</Project>";
}
