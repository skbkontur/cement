using System;
using System.IO;
using System.Xml;
using Cement.Cli.Common.YamlParsers;
using Cement.Cli.Common.Extensions;
using Microsoft.Extensions.Logging;
using net.r_eg.MvsSln;

namespace Cement.Cli.Common;

public sealed class Cleaner
{
    private readonly ShellRunner shellRunner;
    private readonly ILogger<Cleaner> log;
    private readonly ConsoleWriter consoleWriter;

    public Cleaner(ILogger<Cleaner> log, ShellRunner shellRunner, ConsoleWriter consoleWriter)
    {
        this.log = log;
        this.shellRunner = shellRunner;
        this.consoleWriter = consoleWriter;
    }

    public bool IsNetStandard(Dep dep)
    {
        try
        {
            var modulePath = Path.Combine(Helper.CurrentWorkspace, dep.Name);
            var buildSections = Yaml.BuildParser(dep.Name).Get(dep.Configuration);
            foreach (var buildSection in buildSections)
            {
                if (buildSection.Target.IsFakeTarget() || !buildSection.Target.EndsWith(".sln"))
                    continue;

                var slnPath = Path.Combine(modulePath, buildSection.Target);
                using (var sln = new Sln(slnPath, SlnItems.Projects))
                {
                    foreach (var projectItem in sln.Result.ProjectItems)
                    {
                        var csprojPath = projectItem.fullPath;
                        if (IsProjectsTargetFrameworkIsNetStandard(csprojPath))
                            return true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            consoleWriter.WriteInfo($"Could not check TargetFramework in '{dep.Name}'. Continue building without clean");
            log.LogWarning(e, $"An error occured when checking target framework in '{dep.Name}'");
            return false;
        }

        return false;
    }

    public void Clean(Dep dep)
    {
        log.LogInformation($"Start cleaning {dep.Name}");
        consoleWriter.WriteProgress($"Cleaning {dep.Name}");

        // -d               - remove whole directory
        // -f               - force delete
        // -x               - remove ignored files, too
        const string command = "git clean -d -f -x";

        var workingDirectory = Path.Combine(Helper.CurrentWorkspace, dep.Name);
        var timeout = TimeSpan.FromMinutes(1);

        var (exitCode, _, _) = shellRunner.RunInDirectory(workingDirectory, command, timeout, RetryStrategy.None);
        if (exitCode != 0)
        {
            log.LogWarning($"'git clean' finished with non-zero exit code: '{exitCode}'");
            consoleWriter.WriteInfo($"Could not clean {dep.Name}. Continue building without clean");
            return;
        }

        log.LogInformation($"{dep.Name} was cleaned successfully");
        consoleWriter.WriteOk($"{dep.Name} was cleaned successfully");
    }

    private bool IsProjectsTargetFrameworkIsNetStandard(string csprojPath)
    {
        var csprojDocument = new XmlDocument();
        csprojDocument.Load(csprojPath);

        var targetFrameworkNodes = csprojDocument.GetElementsByTagName("TargetFramework");

        foreach (XmlNode item in targetFrameworkNodes)
        {
            var framework = item.InnerText;
            if (framework.Contains("netstandard"))
                return true;
        }

        var targetFrameworksNodes = csprojDocument.GetElementsByTagName("TargetFrameworks");

        foreach (XmlNode item in targetFrameworksNodes)
        {
            var frameworks = item.InnerText;
            if (frameworks.Contains("netstandard"))
                return true;
        }

        return false;
    }
}
