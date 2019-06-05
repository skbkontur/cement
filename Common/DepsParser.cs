using System.Collections.Generic;
using System.IO;
using Common.YamlParsers;

namespace Common
{
    public class DepsParser
    {
        private readonly string modulePath;

        public DepsParser(string modulePath)
        {
            this.modulePath = modulePath;
        }

        public DepsData Get(string config = null)
        {
            if (File.Exists(Path.Combine(modulePath, Helper.YamlSpecFile)))
                return new DepsYamlParser(new FileInfo(modulePath)).Get(config);

            if (File.Exists(Path.Combine(modulePath,
                $"deps{(config == null || config.Equals("full-build") ? "" : "." + config)}")))
                return new DepsIniParser(new FileInfo(Path.Combine(modulePath,
                    $"deps{(config == null || config.Equals("full-build") ? "" : "." + config)}"))).Get();

            if (File.Exists(Path.Combine(modulePath, "deps")))
            {
                ConsoleWriter.WriteWarning("Configuration '" + config + "' was not found in " + modulePath + ". Will take full-build config");
                return new DepsIniParser(Path.Combine(modulePath, "deps")).Get();
            }
            return new DepsData(null, new List<Dep>());
        }
    }
}