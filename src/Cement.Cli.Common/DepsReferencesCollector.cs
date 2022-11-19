using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.YamlParsers;

namespace Cement.Cli.Common;

public sealed class DepsReferencesCollector
{
    private readonly List<Dep> deps;
    private readonly string workspace;

    public DepsReferencesCollector(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory,
                                   string modulePath, string config)
    {
        workspace = Directory.GetParent(modulePath).FullName;
        deps = new DepsYamlParser(consoleWriter, depsValidatorFactory, new FileInfo(modulePath)).Get(config).Deps;
    }

    public DepsReferenceSearchModel GetRefsFromDeps()
    {
        var notFoundInstall = new List<string>();
        var resultInstallData = new List<InstallData>();

        foreach (var dep in deps)
        {
            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, dep.Name)))
            {
                ConsoleWriter.Shared.WriteError("Module " + dep.Name + " not found.");
                continue;
            }

            var depInstall = new InstallCollector(Path.Combine(workspace, dep.Name)).Get(dep.Configuration);
            if (!depInstall.Artifacts.Any())
            {
                if (!Yaml.Exists(dep.Name) || !IsContentModule(dep))
                    notFoundInstall.Add(dep.Name);
            }
            else
            {
                depInstall.ModuleName = dep.Name;
                depInstall.InstallFiles =
                    depInstall.InstallFiles.Select(reference => reference.Replace('/', '\\')).ToList();
                depInstall.Artifacts =
                    depInstall.Artifacts.Select(reference => reference.Replace('/', '\\')).ToList();
                depInstall.CurrentConfigurationInstallFiles =
                    depInstall.CurrentConfigurationInstallFiles.Select(reference => reference.Replace('/', '\\')).ToList();
                resultInstallData.Add(depInstall);
            }
        }

        return new DepsReferenceSearchModel(resultInstallData, notFoundInstall);
    }

    private static bool IsContentModule(Dep dep)
    {
        return Yaml.SettingsParser(dep.Name).Get().IsContentModule || Yaml.BuildParser(dep.Name).Get(dep.Configuration)
            .All(t => t.Target == "None");
    }
}
