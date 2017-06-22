using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    class TestRulesetFile
    {
        private TempDirectory workDirectory = new TempDirectory();

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
        public void Costructor_GetXmlFromFile_IfFileExist()
        {
            var expectedXml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RuleSet Name=\"{Guid.NewGuid()}\" Description=\"{Guid.NewGuid()}\" ToolsVersion=\"10.0\"></RuleSet>";
            var expectedXmlPath = Path.Combine(workDirectory.Path, "expected.ruleset");
            File.WriteAllText(expectedXmlPath, expectedXml);

            var rulesetFile = new RulesetFile(expectedXmlPath);

            Assert.AreEqual(expectedXml, rulesetFile.Document.OuterXml);
        }

        [Test]
        public void Costructor_GetXmlFromTemplate_IfFileNotExist()
        {
            var nullName = Guid.NewGuid().ToString();
            var nullPath = Path.Combine(workDirectory.Path, $"{nullName}.ruleset");
            if (File.Exists(nullPath))
                File.Delete(nullPath);

            var rulesetFile = new RulesetFile(nullPath);

            Console.WriteLine($"Xml should be contains RuleSet with Name '{nullName}':");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            Assert.IsNotNull(SearchByXpath(rulesetFile.Document, $"/a:RuleSet[@Name = '{nullName}']").Single());
        }

        [Test]
        public void Include_AddRulesetPathAsIs_IfRulesetPathIsRelative()
        {
            var dummyPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.ruleset");
            var rulesetFile = new RulesetFile(dummyPath);

            var rulesetPath = "It\\Is\\Relative.ruleset";
            rulesetFile.Include(rulesetPath);

            Console.WriteLine($"Xml should be contains Include with Path '{rulesetPath}':");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            Assert.IsNotNull(SearchByXpath(rulesetFile.Document, $"a:RuleSet/a:Include[@Path = '{rulesetPath}']").Single());
        }

        [Test]
        public void Include_AddRulesetPathAsIs_IfRulesetPathIsMicrosoftRules()
        {
            var dummyPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.ruleset");
            var rulesetFile = new RulesetFile(dummyPath);

            var microsoftRules = "allrules.ruleset";
            rulesetFile.Include(microsoftRules);

            Console.WriteLine($"Xml should be contains Include with Path '{microsoftRules}':");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            Assert.IsNotNull(SearchByXpath(rulesetFile.Document, $"a:RuleSet/a:Include[@Path = '{microsoftRules}']").Single());
        }

        [Test]
        public void Include_AddRulesetPathAsRelative_IfRulesetPathIsAbsolute()
        {
            var dummyPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.ruleset");
            var rulesetFile = new RulesetFile(dummyPath);

            var expectedRelRulesetPath = $"..\\dummyFolder\\{Guid.NewGuid()}.ruleset";
            var absoluteRulesetPath = Path.GetFullPath(Path.Combine(workDirectory.Path, expectedRelRulesetPath));
            rulesetFile.Include(absoluteRulesetPath);

            Console.WriteLine($"Xml should be contains Include with Path '{expectedRelRulesetPath}':");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            Assert.IsNotNull(SearchByXpath(rulesetFile.Document, $"a:RuleSet/a:Include[@Path = '{expectedRelRulesetPath}']").Single());
        }

        [Test]
        public void Include_NotAddRuleset_IfCurrentPathAlreadyIncluded()
        {
            var dummyPath = Path.Combine(workDirectory.Path, $"{Guid.NewGuid()}.ruleset");
            var rulesetFile = new RulesetFile(dummyPath);

            var rulesetPath = $"dummyFolder\\{Guid.NewGuid()}.ruleset";
            rulesetFile.Include(rulesetPath);
            rulesetFile.Include(rulesetPath);
            rulesetFile.Include(rulesetPath);

            Console.WriteLine($"Xml should be contains only one Include with Path '{rulesetPath}':");
            Console.WriteLine(rulesetFile.Document.OuterXml);
            Assert.IsNotNull(SearchByXpath(rulesetFile.Document, $"a:RuleSet/a:Include[@Path = '{rulesetPath}']").Single());
        }


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
