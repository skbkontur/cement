using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Common
{
    public class ProjectFile
    {
        public readonly string LineEndings;

        public readonly string FilePath;
        public readonly XmlDocument Document;
        private readonly bool newFormat;
        private ILog log;

        public ProjectFile(string csprojFilePath)
        {
            var fileContent = File.ReadAllText(csprojFilePath);
            log = LogManager.GetLogger(typeof(ProjectFile));

            LineEndings = fileContent.Contains("\r\n") ? "\r\n" : "\n";
            FilePath = csprojFilePath;
            Document = XmlDocumentHelper.Create(fileContent);
            newFormat = !string.IsNullOrEmpty(Document.DocumentElement?.GetAttribute("Sdk"));
        }

        public void BindRuleset(RulesetFile rulesetFile)
        {
            var relativeRulesetPath = Helper.GetRelativePath(rulesetFile.FilePath, Path.GetDirectoryName(FilePath));
            var oldRuleSets = Document
                .GetElementsByTagName("CodeAnalysisRuleSet")
                .Cast<XmlNode>()
                .ToList();

            if (oldRuleSets.Any(node => node.InnerText == relativeRulesetPath))
                return;

            foreach (var oldRuleSet in oldRuleSets)
            {
                var oldRulesetValue = oldRuleSet.InnerText;
                var oldRulesetPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FilePath), oldRuleSet.InnerText));
                var oldRulesetPathOrName = File.Exists(oldRulesetPath) ? oldRulesetPath : oldRulesetValue;
                rulesetFile.Include(oldRulesetPathOrName);
                oldRuleSet.ParentNode.RemoveChild(oldRuleSet);
            }

            var namespaceUri = Document.DocumentElement.NamespaceURI;
            var nsmgr = new XmlNamespaceManager(Document.NameTable);
            nsmgr.AddNamespace("a", namespaceUri);
            var ruleSetParentNode = Document.SelectSingleNode("/a:Project/a:PropertyGroup[not(@Condition)][a:AssemblyName]", nsmgr);
            var ruleSetNode = Document.CreateElement("CodeAnalysisRuleSet", namespaceUri);
            var ruleSetNodeValue = Document.CreateTextNode(relativeRulesetPath);
            ruleSetNode.AppendChild(ruleSetNodeValue);
            ruleSetParentNode.AppendChild(ruleSetNode);
        }

        public void AddAnalyzer(string analyzerDllPath)
        {
            var analyzerName = Path.GetFileName(analyzerDllPath);
            var analyzerDllRelPath = Helper.GetRelativePath(analyzerDllPath, Path.GetDirectoryName(FilePath));
            var installedAnalyzerNodes = Document
                .GetElementsByTagName("Analyzer")
                .Cast<XmlNode>()
                .ToList();

            if (installedAnalyzerNodes.Any(node => node.Attributes != null && Path.GetFileName(node.Attributes["Include"].Value) == analyzerName))
                return;

            var analyzerGroup = installedAnalyzerNodes.Any() ? installedAnalyzerNodes.First().ParentNode : CreateAnalyzerGroup();
            var analyzerNode = Document.CreateElement("Analyzer", Document.DocumentElement.NamespaceURI);
            analyzerNode.SetAttribute("Include", analyzerDllRelPath);
            analyzerGroup.AppendChild(analyzerNode);
        }

        public bool ContainsRef(string reference, out XmlNode refXml)
        {
            refXml = Document
                .GetElementsByTagName("Reference")
                .Cast<XmlNode>()
                .FirstOrDefault(node => node.Attributes?["Include"].Value != null && node.Attributes["Include"].Value.Split(',').First().Trim() == reference);
            return refXml != null;
        }

        public XmlNode CreateReference(string refName, string refPath)
        {
            var elementToInsert = Document.CreateElement("Reference", Document.DocumentElement.NamespaceURI);
            elementToInsert.SetAttribute("Include", refName);

            var specificVersion = Document.CreateElement("SpecificVersion", Document.DocumentElement.NamespaceURI);
            specificVersion.InnerText = "False";

            var hintPath = Document.CreateElement("HintPath", Document.DocumentElement.NamespaceURI);
            hintPath.InnerText = refPath;

            elementToInsert.AppendChild(specificVersion);
            elementToInsert.AppendChild(hintPath);

            return elementToInsert;
        }
        public XmlNode CreateNuGetReference(string refName, string version)
        {
            var elementToInsert = Document.CreateElement("PackageReference", Document.DocumentElement.NamespaceURI);
            elementToInsert.SetAttribute("Include", refName);
            elementToInsert.SetAttribute("Version", version);
            return elementToInsert;
        }

        public void AddRef(string refName, string refPath)
        {
            try
            {
                var referenceGroup = Document
                    .GetElementsByTagName("ItemGroup")
                    .Cast<XmlNode>()
                    .FirstOrDefault(IsReferenceGroup);

                if (referenceGroup == null)
                    referenceGroup = CreateItemGroup();

                if (referenceGroup != null)
                    referenceGroup.AppendChild(CreateReference(refName, refPath));
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to add ref {refName} to {FilePath}", e);
            }
        }

        public void ReplaceRef(string refName, string refPath)
        {
            try
            {
                var itemGroups = Document.GetElementsByTagName("ItemGroup")
                    .Cast<XmlNode>()
                    .Where(g => g.HasChildNodes)
                    .ToList();

                foreach (var referenceGroup in itemGroups)
                {
                    var toReplace = new List<XmlNode>();
                    foreach (XmlNode node in referenceGroup.ChildNodes)
                    {
                        if (node.Attributes != null && node.Attributes["Include"]?.Value.Split(',').First().Trim() == refName)
                        {
                            toReplace.Add(node);
                        }
                    }

                    foreach (var node in toReplace)
                    {
                        referenceGroup.ReplaceChild(CreateReference(refName, refPath), node);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to replace ref {refName} in {FilePath}", e);
            }
        }

        public XmlDocument CreateCsProjWithNugetReferences(List<Dep> deps, bool preRelease)
        {
            if (!newFormat)
                throw new Exception("Only new csproj format supported");
            var fileContent = File.ReadAllText(FilePath);
            var patchedProjDoc = XmlDocumentHelper.Create(fileContent);
            var genPackageNode = patchedProjDoc.SelectSingleNode("//PropertyGroup/GeneratePackageOnBuild");
            genPackageNode?.ParentNode?.RemoveChild(genPackageNode);
            var itemGroup = patchedProjDoc.CreateElement("ItemGroup");
            if (patchedProjDoc.DocumentElement == null)
                throw new Exception("DocumentElement is null at csproj");
            patchedProjDoc.DocumentElement?.AppendChild(itemGroup);

            var nugetRunCommand = NuGetHelper.GetNugetRunCommand();

            foreach (var dep in deps)
            {
                var refNodes = patchedProjDoc.SelectNodes("//Reference");
                if (refNodes != null)
                {
                    var node = refNodes.Cast<XmlNode>().FirstOrDefault(x =>
                    {
                        var moduleName = x.Attributes?["Include"]?.Value;
                        return moduleName != null && moduleName.Equals(dep.Name, StringComparison.InvariantCultureIgnoreCase);
                    });
                    node?.ParentNode?.RemoveChild(node);
                }
                var refElement = patchedProjDoc.CreateElement("PackageReference");
                var includeAttr = patchedProjDoc.CreateAttribute("Include");
                includeAttr.Value = dep.Name;
                refElement.Attributes.Append(includeAttr);
                var packageVersion = NuGetHelper.GetNugetPackageVersion(dep.Name, nugetRunCommand, preRelease);
                if (!string.IsNullOrEmpty(packageVersion))
                {
                    var versionAttr = patchedProjDoc.CreateAttribute("Version");
                    versionAttr.Value = packageVersion;
                    refElement.Attributes.Append(versionAttr);
                }
                itemGroup.AppendChild(refElement);
            }

            return patchedProjDoc;
        }
        
        public void Save()
        {
            XmlDocumentHelper.Save(Document, FilePath, LineEndings);
        }

        private XmlNode CreateAnalyzerGroup()
        {
            var namespaceUri = Document.DocumentElement.NamespaceURI;
            var namespaceManager = new XmlNamespaceManager(Document.NameTable);
            namespaceManager.AddNamespace("a", namespaceUri);
            var analyzerGroupParent = Document.SelectSingleNode("/a:Project", namespaceManager);
            var analyzerGroupNeighbor = analyzerGroupParent.SelectSingleNode("a:ItemGroup", namespaceManager);
            var analyzerGroup = Document.CreateElement("ItemGroup", namespaceUri);
            analyzerGroupParent.InsertBefore(analyzerGroup, analyzerGroupNeighbor);
            return analyzerGroup;
        }

        private bool IsReferenceGroup(XmlNode xmlNode)
        {
            return xmlNode.ChildNodes
                .Cast<XmlNode>()
                .Any(childNode => childNode.Name == "Reference");
        }
        private bool IsPackageReferenceGroup(XmlNode xmlNode)
        {
            return xmlNode.ChildNodes
                .Cast<XmlNode>()
                .Any(childNode => childNode.Name == "PackageReference");
        }

        private XmlNode CreateItemGroup()
        {
            var rootNode = Document
                .GetElementsByTagName("Project")
                .Cast<XmlNode>()
                .FirstOrDefault();
            if (rootNode == null)
            {
                ConsoleWriter.WriteError("Really bad cspoj :(");
                return null;
            }

            var itemGroup = Document.CreateElement("ItemGroup", Document.DocumentElement.NamespaceURI);
            rootNode.AppendChild(itemGroup);

            return itemGroup;
        }

        public void InstallNuGetPackages(List<string> nuGetPackages)
        {
            if (newFormat)
            {
                foreach (var package in nuGetPackages)
                {
                    var splitted = package.Split('/');
                    if (splitted.Length != 2)
                    {
                        log.Error("package version is not defined: " + package);
                    }
                    else
                    {
                        InstallNuGetPackage(splitted[0], splitted[1]);
                    }
                }
            }
            else
            {
                var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
                var packagesDirectory = Path.Combine(currentModuleDirectory, "packages");
                new NuGetPackageHepler(log).InstallPackages(nuGetPackages, packagesDirectory, this);
            }
        }

        private void InstallNuGetPackage(string packageName, string packageVersion)
        {
            try
            {
                var referenceGroup = Document
                                         .GetElementsByTagName("ItemGroup")
                                         .Cast<XmlNode>()
                                         .FirstOrDefault(IsPackageReferenceGroup) ?? CreateItemGroup();
                var packageNode = Document.SelectNodes("*/ItemGroup/PackageReference")?.Cast<XmlElement>()
                    .FirstOrDefault(el => el.Attributes["Include", Document.DocumentElement.NamespaceURI].Value.Equals(packageName, StringComparison.InvariantCultureIgnoreCase));
                if (packageNode == null)
                    referenceGroup?.AppendChild(CreateNuGetReference(packageName, packageVersion));
                else
                {
                    packageNode.ParentNode?.RemoveChild(packageNode);
                    packageNode.SetAttribute("Version", packageVersion);
                    referenceGroup?.AppendChild(packageNode);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to add ref {packageName} to {FilePath}", e);
            }
        }
    }
}
