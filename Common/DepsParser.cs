using System.Collections.Generic;
using System.IO;
using Common.DepsValidators;
using Common.YamlParsers;

namespace Common
{
    public sealed class DepsParser
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly IDepsValidatorFactory depsValidatorFactory;
        private readonly string modulePath;

        public DepsParser(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory, string modulePath)
        {
            this.consoleWriter = consoleWriter;
            this.depsValidatorFactory = depsValidatorFactory;
            this.modulePath = modulePath;
        }

        public DepsData Get(string config = null)
        {
            if (File.Exists(Path.Combine(modulePath, Helper.YamlSpecFile)))
            {
                return new DepsYamlParser(consoleWriter, depsValidatorFactory, new FileInfo(modulePath)).Get(config);
            }

            var path = $"deps{(config is null or "full-build" ? "" : "." + config)}";
            if (File.Exists(Path.Combine(modulePath, path)))
            {
                return new DepsIniParser(new FileInfo(Path.Combine(modulePath, path))).Get();
            }

            if (File.Exists(Path.Combine(modulePath, "deps")))
            {
                consoleWriter.WriteWarning("Configuration '" + config + "' was not found in " + modulePath + ". Will take full-build config");
                return new DepsIniParser(Path.Combine(modulePath, "deps")).Get();
            }

            return new DepsData(null, new List<Dep>());
        }
    }
}
