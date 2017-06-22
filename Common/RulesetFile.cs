using System.IO;
using System.Linq;
using System.Xml;

namespace Common
{
    public class RulesetFile
    {
        private readonly string lineEndings;

        public readonly string FilePath;
        public readonly XmlDocument Document;

        public RulesetFile(string rulesetFilePath)
        {
            string fileContent;
            if (File.Exists(rulesetFilePath))
            {
                fileContent = File.ReadAllText(rulesetFilePath);
            }
            else
            {
                var name = Path.GetFileNameWithoutExtension(rulesetFilePath);
                fileContent =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""{name}"" Description=""Ruleset for project {name}.csproj"" ToolsVersion=""10.0"">
</RuleSet>";
            }

            lineEndings = fileContent.Contains("\r\n") ? "\r\n" : "\n";
            FilePath = rulesetFilePath;
            Document = XmlDocumentHelper.Create(fileContent);
        }

        public void Include(string rulesetPath)
        {
            var relativeRulesetPath = Path.IsPathRooted(rulesetPath) ? Helper.GetRelativePath(rulesetPath, Path.GetDirectoryName(FilePath)) : rulesetPath;

            if (!AlreadyIncluded(relativeRulesetPath))
            {
                var nodeParent = Document
                    .GetElementsByTagName("RuleSet")
                    .Cast<XmlNode>()
                    .First();

                var node = Document.CreateElement("Include", Document.DocumentElement.NamespaceURI);
                node.SetAttribute("Path", relativeRulesetPath);
                node.SetAttribute("Action", "Default");
                nodeParent.AppendChild(node);
            }
        }

        private bool AlreadyIncluded(string rulesetPath)
        {
            return Document
                .GetElementsByTagName("Include")
                .Cast<XmlNode>()
                .Any(node => node.Attributes != null && node.Attributes["Path"].Value == rulesetPath);
        }

        public void Save()
        {
            XmlDocumentHelper.Save(Document, FilePath, lineEndings);
        }
    }
}