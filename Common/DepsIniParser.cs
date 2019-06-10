using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;

namespace Common
{
    public class DepsIniParser
    {
        private readonly IniParser parser;
        private readonly string content;

        public DepsIniParser(string content)
        {
            parser = new IniParser();
            this.content = content;
        }

        public DepsIniParser(FileInfo filePath)
        {
            parser = new IniParser();
            content = File.ReadAllText(filePath.ToString());
        }

        public DepsData Get()
        {
            var parsed = parser.ParseString(content);

            var force = GetForceFromIni(parsed);
            var deps = GetDepsFromIni(parsed);

            return new DepsData(force, deps);
        }

        private List<Dep> GetDepsFromIni(IniData parsed)
        {
            var moduleSections = parsed.GetSections().Where(section => section.StartsWith("module")).ToList();
            var deps = new List<Dep>();
            foreach (var section in moduleSections)
            {
                var depName = section.Substring("module ".Length);
                var depTreeish = parsed.GetValue("treeish", section);
                depTreeish = depTreeish.Equals("") ? parsed.GetValue("treesh", section) : depTreeish;
                depTreeish = depTreeish.Equals("") ? null : depTreeish;
                var depConfiguration = parsed.GetValue("config", section);
                depConfiguration = depConfiguration.Equals("")
                    ? parsed.GetValue("configuration", section)
                    : depConfiguration;
                depConfiguration = depConfiguration.Equals("") ? parsed.GetValue("conf", section) : depConfiguration;
                depConfiguration = depConfiguration.Equals("") ? null : depConfiguration;
                deps.Add(new Dep(depName, depTreeish, depConfiguration));
            }

            return deps;
        }

        private string[] GetForceFromIni(IniData parsed)
        {
            var force = parsed.GetValue("force", "main");
            return force == "" ? null : force.Split(',');
        }
    }
}