using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public class DepsChecker
    {
        private readonly List<BuildData> buildData;
        private readonly DepsReferencesCollector depsRefsCollector;
        private readonly List<string> modules;
        private readonly string moduleDirectory;

        public DepsChecker(string cwd, string config, List<Module> modules)
        {
            if (!new ConfigurationParser(new FileInfo(cwd)).ConfigurationExists(config))
                throw new NoSuchConfigurationException(cwd, config);
            buildData = new BuildYamlParser(new FileInfo(cwd)).Get(config);
            depsRefsCollector = new DepsReferencesCollector(cwd, config);
            this.modules = modules.Select(m => m.Name).ToList();
            moduleDirectory = cwd;
        }

        public CheckDepsResult GetCheckDepsResult()
        {
            var refsList = new List<ReferenceWithCsproj>();
            foreach (var bulid in buildData)
            {
                if (bulid.Target.IsFakeTarget())
                    continue;
                var vsParser = new VisualStudioProjectParser(Path.Combine(moduleDirectory, bulid.Target), modules);
                var files = vsParser.GetCsprojList(bulid.Configuration);
                var refs = files.SelectMany(file =>
                    vsParser.GetReferencesFromCsproj(file).Select(reference => reference.Replace('/', '\\')).Select(r => new ReferenceWithCsproj(r, file)));
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
                foreach (var d in installData.MainConfigBuildFiles)
                {
                    if (csprojRefs.Any(r => r.Reference.ToLower() == d.ToLower()))
                        isOverhead = false;
                }
                if (isOverhead)
                    configOverhead.Add(installData.ModuleName);
            }

            var lowerInDeps = inDeps.Select(r => r.ToLower()).ToList();
            var notInDeps = csprojRefs.Where(r => !lowerInDeps.Contains(r.Reference.ToLower())).ToList();
            foreach (var r in csprojRefs)
            {
                var moduleName = r.Reference.Split('\\')[0];
                notUsedDeps.Remove(moduleName);
            }

            DeleteMsBuild(notUsedDeps);
            DeleteMsBuild(configOverhead);
            return new CheckDepsResult(notUsedDeps, notInDeps, noYamlInstall, configOverhead);
        }

        private void DeleteMsBuild(SortedSet<string> refs)
        {
            refs.RemoveWhere(r => r.StartsWith("msbuild/") || r.StartsWith("msbuild\\"));
            refs.RemoveWhere(r => r.StartsWith("nuget/") || r.StartsWith("nuget\\"));
        }
    }

    public class ReferenceWithCsproj
    {
        public readonly string CsprojFile;
        public readonly string Reference;

        public ReferenceWithCsproj(string reference, string csprojFile)
        {
            Reference = reference;
            CsprojFile = csprojFile;
        }
    }

    public class CheckDepsResult
    {
        public readonly SortedSet<string> NotUsedDeps;
        public readonly List<ReferenceWithCsproj> NotInDeps;
        public readonly SortedSet<string> NoYamlInstallSection;
        public readonly SortedSet<string> ConfigOverhead;

        public CheckDepsResult(SortedSet<string> notUsedDeps, List<ReferenceWithCsproj> notInDeps,
            SortedSet<string> noYamlInstall, SortedSet<string> configOverhead)
        {
            NotUsedDeps = notUsedDeps;
            NotInDeps = notInDeps;
            NoYamlInstallSection = noYamlInstall;
            ConfigOverhead = configOverhead;
        }
    }

    public class DepsReferencesCollector
    {
        private readonly List<Dep> deps;
        private readonly string workspace;

        public DepsReferencesCollector(string modulePath, string config)
        {
            workspace = Directory.GetParent(modulePath).FullName;
            deps = new DepsYamlParser(new FileInfo(modulePath)).Get(config).Deps;
        }

        public DepsReferenceSearchModel GetRefsFromDeps()
        {
            var notFoundInstall = new List<string>();
            var resultInstallData = new List<InstallData>();

            foreach (var dep in deps)
            {
                if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, dep.Name)))
                {
                    ConsoleWriter.WriteError("Module " + dep.Name + " not found.");
                    continue;
                }

                var depInstall = new InstallCollector(Path.Combine(workspace, dep.Name)).Get(dep.Configuration);
                if (!depInstall.Artifacts.Any())
                {
                    if (!Yaml.Exists(dep.Name) || !IsContentModuel(dep))
                        notFoundInstall.Add(dep.Name);
                }
                else
                {
                    depInstall.ModuleName = dep.Name;
                    depInstall.BuildFiles =
                        depInstall.BuildFiles.Select(reference => reference.Replace('/', '\\')).ToList();
                    depInstall.Artifacts =
                        depInstall.Artifacts.Select(reference => reference.Replace('/', '\\')).ToList();
                    depInstall.MainConfigBuildFiles =
                        depInstall.MainConfigBuildFiles.Select(reference => reference.Replace('/', '\\')).ToList();
                    resultInstallData.Add(depInstall);
                }
            }
            return new DepsReferenceSearchModel(resultInstallData, notFoundInstall);
        }

        private static bool IsContentModuel(Dep dep)
        {
            return Yaml.SettingsParser(dep.Name).Get().IsContentModule || Yaml.BuildParser(dep.Name).Get(dep.Configuration)
                .All(t => t.Target == "None");
        }
    }

    public class DepsReferenceSearchModel
    {
        public List<InstallData> FoundReferences;
        public List<string> NotFoundInstallSection;

        public DepsReferenceSearchModel(List<InstallData> found, List<string> notFound)
        {
            FoundReferences = found;
            NotFoundInstallSection = notFound;
        }
    }
}