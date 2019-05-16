using System.Collections.Generic;
using System.Linq;

namespace Common.YamlParsers
{
    public class BuildSectionParser
    {
        private readonly CementSettings settings;
        private static readonly Tool defaultTool = new Tool("msbuild");

        public BuildSectionParser() : this(CementSettings.Get())
        {
        }

        public BuildSectionParser(CementSettings settings)
        {
            this.settings = settings;
        }

        public BuildData[] ParseBuildSections(string module, params IDictionary<object, object>[] buildSections)
        {
            var count = buildSections.Length;
            if (count == 0)
                return new BuildData[0];

            var result = new List<BuildData>();
            foreach (var section in buildSections)
            {
                var target = Helper.FixPath(FindValue(section, "target", string.Empty));
                var configuration = FindValue(section, "configuration");
                var tool = GetTools(module, section);
                var parameters = GetBuildParams(section);
                var name = FindValue(section, "name", string.Empty);

                if (target.EndsWith(".sln") && string.IsNullOrEmpty(configuration))
                    throw new BadYamlException(module, "build", "Build configuration not found");

                if (count > 1 && string.IsNullOrEmpty(name))
                    throw new CementException("Multiple parts of build-section require names");

                result.Add(new BuildData(target, configuration, tool, parameters, name));
            }

            return result.ToArray();
        }

        private Tool GetTools(string module, IDictionary<object, object> section)
        {
            if (!section.TryGetValue("tool", out var tool))
                return defaultTool;

            switch (tool)
            {
                case string toolString when string.IsNullOrEmpty(toolString):
                    throw new BadYamlException(module, "tool", "empty tool");

                case string toolString:
                    return new Tool(toolString);

                case IDictionary<object, object> toolDict:
                    var name = FindValue(toolDict, "name", "msbuild");
                    var defaultVersion = name == "msbuild" ? settings.DefaultMsBuildVersion : null;
                    var version = FindValue(toolDict, "version", defaultVersion);
                    return new Tool(name,version);

                default:
                    throw new BadYamlException(module, "tool", "not dict format");
            }
        }

        private List<string> GetBuildParams(IDictionary<object, object> section)
        {
            var buildParams = FindValue<object>(section, "parameters");
            switch (buildParams)
            {
                case List<object> list:
                    return list.Cast<string>().ToList();
                case string str:
                    return new List<string>() { str };
                default:
                    return new List<string>();
            }
        }

        private string FindValue(IDictionary<object, object> dict, string key, string defaultValue = null)
        {
            return FindValue<string>(dict, key, defaultValue);
        }

        private T FindValue<T>(IDictionary<object, object> dict, string key, T defaultValue = default(T))
        {
            return dict.ContainsKey(key) ? (T)dict[key] : defaultValue;
        }
    }
}