using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Common.Extensions;
using Common.Logging;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;
using net.r_eg.MvsSln;

namespace Common
{
    public sealed class Cleaner
    {
        private readonly IShellRunner shellRunner;
        private readonly ILogger<Cleaner> log = LogManager.GetLogger<Cleaner>();

        public Cleaner(IShellRunner shellRunner)
        {
            this.shellRunner = shellRunner;
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
                ConsoleWriter.WriteInfo($"Could not check TargetFramework in '{dep.Name}'. Continue building without clean");
                log.LogWarning(e, $"An error occured when checking target framework in '{dep.Name}'");
                return false;
            }

            return false;
        }

        public void Clean(Dep dep)
        {
            log.LogInformation($"Start cleaning {dep.Name}");
            ConsoleWriter.WriteProgress($"Cleaning {dep.Name}");

            var command = "git clean -d -f -x"; // -d               - remove whole directory
                                                // -f               - force delete
                                                // -x               - remove ignored files, too 

            var exitCode = shellRunner.RunInDirectory(Path.Combine(Helper.CurrentWorkspace, dep.Name), command, TimeSpan.FromMinutes(1), RetryStrategy.None);

            if (exitCode != 0)
            {
                log.LogWarning($"'git clean' finished with non-zero exit code: '{exitCode}'");
                ConsoleWriter.WriteInfo($"Could not clean {dep.Name}. Continue building without clean");
                return;
            }

            log.LogInformation($"{dep.Name} was cleaned successfully");
            ConsoleWriter.WriteOk($"{dep.Name} was cleaned successfully");
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
}
