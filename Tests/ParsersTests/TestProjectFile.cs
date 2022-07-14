using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    internal class TestProjectFile
    {
        private readonly string defaultCsprojXml =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
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
      <HintPath>..\..\lalala\LalalaReference.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include=""Other.dll"" />
    <Analyzer Include=""dummyDir\Another.dll"" />
  </ItemGroup>
</Project>";
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
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
            XmlNode refXml;
            Assert.IsTrue(proj.ContainsRef("log4net", out refXml));
            Assert.IsFalse(proj.ContainsRef("logging", out refXml));
        }

        [Test]
        public void TestAddReference()
        {
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
            XmlNode refXml;
            var proj = CreateProjectFile(content);
            proj.AddRef("logging", "abc/def");
            Assert.IsTrue(proj.ContainsRef("logging", out refXml));
            Assert.AreEqual("abc/def", refXml.LastChild.InnerText);
        }

        [Test]
        public void TestAddReferenceInEmptyProject()
        {
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"">
</Project>
";
            XmlNode refXml;
            var proj = CreateProjectFile(content);
            proj.AddRef("logging", "abc/def");
            Assert.IsTrue(proj.ContainsRef("logging", out refXml));
            Assert.AreEqual("abc/def", refXml.LastChild.InnerText);
        }

        [Test]
        public void TestAddReferenceReferenceNotFirst()
        {
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
            XmlNode refXml;
            var proj = CreateProjectFile(content);
            proj.AddRef("logging", "abc/def");
            Assert.IsTrue(proj.ContainsRef("logging", out refXml));
            Assert.AreEqual("abc/def", refXml.LastChild.InnerText);
        }

        [Test]
        public void TestReplaceReferencePath()
        {
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
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
            XmlNode refXml;
            var proj = CreateProjectFile(content);
            proj.ReplaceRef("log4net", "abc/def");
            Assert.IsTrue(proj.ContainsRef("log4net", out refXml));
            Assert.AreEqual("abc/def", refXml.LastChild.InnerText);
        }

        [Test]
        public void Costructor_GetXmlFromFile_IfFileExist()
        {
            var projectFile = CreateProjectFile(defaultCsprojXml);

            Assert.AreEqual(WithoutXmlFormatting(defaultCsprojXml), projectFile.Document.OuterXml);
        }

        [Test]
        public void Costructor_ThrowException_IfFileNotExist()
        {
            var nullName = Guid.NewGuid().ToString();
            var nullPath = Path.Combine(workDirectory.Path, $"{nullName}.csproj");
            if (File.Exists(nullPath))
                File.Delete(nullPath);

            Assert.Throws<FileNotFoundException>(() => new ProjectFile(nullPath));
        }

        [Test]
        public void BindRuleset_NoBindRuleset_IfBindingAlreadyExists()
        {
            var rulesetName = "Other.ruleset";
            var rulesetPath = Path.Combine(workDirectory.Path, rulesetName);
            var rulesetFile = new RulesetFile(rulesetPath);
            var projectFile = CreateProjectFile(defaultCsprojXml);

            projectFile.BindRuleset(rulesetFile);

            Console.WriteLine("ProjectFile should be contains 2 old rulesetBindings:");
            Console.WriteLine(projectFile.Document.OuterXml);
            var rulesetBindings = SearchByXpath(projectFile.Document, "//a:CodeAnalysisRuleSet");
            Assert.AreEqual(2, rulesetBindings.Count);
        }

        [Test]
        public void BindRuleset_MoveOldRulesetBindings_FromProjectFileToRulesetFile()
        {
            var rulesetName = $"{Guid.NewGuid()}.ruleset";
            var rulesetPath = Path.Combine(workDirectory.Path, rulesetName);
            var rulesetFile = new RulesetFile(rulesetPath);
            var projectFile = CreateProjectFile(defaultCsprojXml);

            projectFile.BindRuleset(rulesetFile);

            Console.WriteLine($"ProjectFile should be contains only one CodeAnalysisRuleSet with value '{rulesetName}':");
            Console.WriteLine(projectFile.Document.OuterXml);
            var rulesetBinding = SearchByXpath(projectFile.Document, "//a:CodeAnalysisRuleSet").Single();
            Assert.AreEqual(rulesetName, rulesetBinding.InnerText);

            Console.WriteLine("RulesetFile should be contains oldRulesetBindings:");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            var oldRulesetBindings = SearchByXpath(rulesetFile.Document, "//a:Include");
            Assert.AreEqual(2, oldRulesetBindings.Count);
        }

        [Test]
        public void AddAnalyzer_CreateItemGroupForAnalyzers_IfNotExists()
        {
            var analyzerDllPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.dll");
            var csprojContent = @"<?xml version=""1.0"" encoding=""utf-8""?><Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""></Project>";
            var projectFile = CreateProjectFile(csprojContent);

            projectFile.AddAnalyzer(analyzerDllPath);

            Assert.NotNull(SearchByXpath(projectFile.Document, "//a:ItemGroup[a:Analyzer]").Single());
        }

        [Test]
        public void AddAnalyzer_AddAnalyzersDll_IfNotExists()
        {
            var analyzerDllRelpath = $"{Guid.NewGuid()}.dll";
            var analyzerDllFullPath = Path.Combine(workDirectory.Path, analyzerDllRelpath);
            var projectFile = CreateProjectFile(defaultCsprojXml);

            projectFile.AddAnalyzer(analyzerDllFullPath);

            Assert.NotNull(SearchByXpath(projectFile.Document, $"//a:ItemGroup/a:Analyzer[@Include = '{analyzerDllRelpath}']").Single());
        }

        [Test]
        public void AddAnalyzer_NotAddAnalyzersDll_IfDllWithSomeNameAlreadyAdded()
        {
            var analyzerDllRelpath = "Another.dll";
            var analyzerDllFullPath = Path.Combine(workDirectory.Path, analyzerDllRelpath);
            var projectFile = CreateProjectFile(defaultCsprojXml);

            projectFile.AddAnalyzer(analyzerDllFullPath);

            Assert.AreEqual(0, SearchByXpath(projectFile.Document, "//a:ItemGroup/a:Analyzer[@Include = 'Another.dll']").Count);
            Assert.AreEqual(1, SearchByXpath(projectFile.Document, "//a:ItemGroup/a:Analyzer[@Include = 'dummyDir\\Another.dll']").Count);
        }

        [Test]
        public void TestMakeCsProjWithNugetReferences()
        {
            var content = @"<Project Sdk=""Microsoft.NET.Sdk"">

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

            Assert.Null(xmlDocument.SelectSingleNode("//Reference[@Include='Vostok.Core']"));
            Assert.NotNull(xmlDocument.SelectSingleNode("//PackageReference[@Include='vostok.core']"));
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

        private string WithoutXmlFormatting(string xml) => new Regex(">\\s+<").Replace(xml, "><");

        private List<XmlNode> SearchByXpath(XmlDocument xmlDocument, string xpath)
        {
            var namespaceUri = xmlDocument.DocumentElement.NamespaceURI;
            var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("a", namespaceUri);
            var result = xmlDocument
                .SelectNodes(xpath, nsmgr)
                .Cast<XmlNode>()
                .ToList();
            return result;
        }
    }
}
