using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers;

namespace Common
{
    public sealed class DepsChecker
    {
        private readonly List<BuildData> buildData;
        private readonly DepsReferencesCollector depsRefsCollector;
        private readonly List<string> modules;
        private readonly string moduleDirectory;
        private readonly string moduleName;

        public DepsChecker(string cwd, string config, List<Module> modules)
        {
            if (!new ConfigurationParser(new FileInfo(cwd)).ConfigurationExists(config))
                throw new NoSuchConfigurationException(cwd, config);
            buildData = new BuildYamlParser(new FileInfo(cwd)).Get(config);
            depsRefsCollector = new DepsReferencesCollector(cwd, config);
            this.modules = modules.Select(m => m.Name).ToList();
            moduleDirectory = cwd;
            moduleName = Path.GetFileName(moduleDirectory);
        }

        public CheckDepsResult GetCheckDepsResult(bool notOnlyCement)
        {
            var refsList = new List<ReferenceWithCsproj>();
            foreach (var bulid in buildData)
            {
                if (bulid.Target.IsFakeTarget() || (bulid.Tool.Name != "msbuild" && bulid.Tool.Name != "dotnet"))
                    continue;
                var vsParser = new VisualStudioProjectParser(Path.Combine(moduleDirectory, bulid.Target), modules);
                var files = vsParser.GetCsprojList(bulid);
                var refs = files.SelectMany(
                    file =>
                        vsParser.GetReferencesFromCsproj(file, bulid.Configuration, notOnlyCement).Select(reference => reference.Replace('/', '\\')).Select(r => new ReferenceWithCsproj(r, file)));
                refsList.AddRange(refs);
            }

            return GetCheckDepsResult(refsList);
        }

        private CheckDepsResult GetCheckDepsResult(List<ReferenceWithCsproj> csprojRefs)
        {
            var depsInstalls = depsRefsCollector.GetRefsFromDeps();
            var noYamlInstall = new SortedSet<string>(depsInstalls.NotFoundInstallSection);
            var inDeps = new SortedSet<string>();
            var notUsedDeps = new SortedSet<string>();
            var configOverhead = new SortedSet<string>();
            foreach (var installData in depsInstalls.FoundReferences)
            {
                notUsedDeps.Add(installData.ModuleName);

                foreach (var d in installData.Artifacts)
                {
                    inDeps.Add(d);
                }

                bool isOverhead = true;
                foreach (var d in installData.CurrentConfigurationInstallFiles)
                {
                    if (csprojRefs.Any(r => r.Reference.ToLower() == d.ToLower()))
                        isOverhead = false;
                }

                if (isOverhead)
                    configOverhead.Add(installData.ModuleName);
            }

            var lowerInDeps = inDeps.Select(r => r.ToLower()).ToList();
            var notInDeps = csprojRefs
                .Where(r => !lowerInDeps.Contains(r.Reference.ToLower()))
                .Where(r => GetModuleName(r.Reference) != moduleName)
                .ToList();

            var innerRefs = csprojRefs
                .Where(r => GetModuleName(r.Reference) == moduleName)
                .Where(r => !r.Reference.ToLower().Contains("\\packages\\"))
                .ToList();
            var allInstalls = new HashSet<string>(
                InstallHelper.GetAllInstallFiles().Select(Path.GetFileName));
            notInDeps.AddRange(innerRefs.Where(i => allInstalls.Contains(Path.GetFileName(i.Reference))));

            foreach (var r in csprojRefs)
            {
                var moduleName = GetModuleName(r.Reference);
                notUsedDeps.Remove(moduleName);
            }

            DeleteMsBuild(notUsedDeps);
            DeleteMsBuild(configOverhead);
            return new CheckDepsResult(notUsedDeps, notInDeps, noYamlInstall, configOverhead);
        }

        private string GetModuleName(string reference)
        {
            return reference.Split('\\')[0];
        }

        private void DeleteMsBuild(SortedSet<string> refs)
        {
            refs.RemoveWhere(r => r == "msbuild" || r.StartsWith("msbuild/") || r.StartsWith("msbuild\\"));
            refs.RemoveWhere(r => r == "nuget" || r.StartsWith("nuget/") || r.StartsWith("nuget\\"));
        }
    }
}
