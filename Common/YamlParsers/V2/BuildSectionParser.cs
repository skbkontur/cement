using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class BuildSectionParser
    {
        private readonly CementSettings settings;
        private const string DefaultToolName = "msbuild";

        public BuildSectionParser() : this(CementSettings.Get())
        {
        }

        public BuildSectionParser(CementSettings settings)
        {
            this.settings = settings;
        }

        [CanBeNull]
        public BuildData ParseDefaults([CanBeNull] object contents)
        {
            var result = ParseInner(contents);
            if (result != null && result.Length > 1)
                throw new CementException("Default configuration can't contains multiple build sections.");

            return result?.FirstOrDefault();
        }

        [NotNull]
        public BuildData[] ParseConfiguration([CanBeNull] object contents, BuildData defaults = null)
        {
            var result = ParseInner(contents, defaults);
            if (result.Any(s => s.Target.EndsWith(".sln") && string.IsNullOrEmpty(s.Configuration)))
                throw new BadYamlException("build", "Build configuration not found. You have to explicitly specify 'configuration' for *.sln targets.");
            return result;
        }

        [NotNull]
        public BuildData[] ParseInner([CanBeNull] object contents, BuildData defaults = null)
        {
            var buildSections = CastContent(contents);
            if (buildSections == null || buildSections.Length == 0)
                return defaults == null ? new BuildData[0] : new [] { defaults };

            var result = new List<BuildData>();

            var defaultTarget = string.IsNullOrEmpty(defaults?.Target) ? string.Empty : defaults.Target;
            var defaultConfiguration = string.IsNullOrEmpty(defaults?.Configuration) ? null : defaults.Configuration;
            var defaultToolName = string.IsNullOrEmpty(defaults?.Tool?.Name) ? DefaultToolName : defaults.Tool.Name;
            var defaultToolVersion = string.IsNullOrEmpty(defaults?.Tool?.Version) ? null : defaults.Tool.Version;
            var defaultParams = defaults?.Parameters ?? new List<string>();
            var defaultName = defaults?.Name ?? string.Empty;

            foreach (var section in buildSections)
            {
                var target = Helper.FixPath(FindValue(section, "target", defaultTarget));
                var configuration = FindValue(section, "configuration", defaultConfiguration);
                var tool = GetTools(section, defaultToolName, defaultToolVersion);
                var parameters = FindBuildParams(section) ?? defaultParams;
                var name = FindValue(section, "name", defaultName);

                if (buildSections.Length > 1 && string.IsNullOrEmpty(name))
                    throw new CementException("Multiple parts of build-section require names");

                result.Add(new BuildData(target, configuration, tool, parameters, name));
            }

            return result.ToArray();
        }

        private Tool GetTools(IDictionary<object, object> section, string defaultName, string versionFromDefault)
        {
            if (!section.TryGetValue("tool", out var tool))
                return new Tool(defaultName, versionFromDefault);

            switch (tool)
            {
                case string toolString when string.IsNullOrEmpty(toolString):
                    throw new BadYamlException("tool", "empty tool specified in 'build' section ('tool' subsection).'");

                case string toolString:
                    return new Tool(toolString);

                case IDictionary<object, object> toolDict:
                    var name = FindValue(toolDict, "name", defaultName);
                    var defaultVersion = name == "msbuild" && string.IsNullOrEmpty(versionFromDefault)
                        ? settings.DefaultMsBuildVersion
                        : versionFromDefault;

                    var version = FindValue(toolDict, "version", defaultVersion);
                    return new Tool(name,version);

                default:
                    throw new BadYamlException("tool", "not dict format");
            }
        }

        [CanBeNull]
        private List<string> FindBuildParams(IDictionary<object, object> section)
        {
            var buildParams = FindValue<object>(section, "parameters");
            switch (buildParams)
            {
                case List<object> list:
                    return list.Cast<string>().ToList();
                case string str:
                    return new List<string> { str };
                default:
                    return null;
            }
        }

        private IDictionary<object, object>[] CastContent(object contents)
        {
            switch (contents)
            {
                case null:
                    return null;
                case string s when string.IsNullOrEmpty(s):
                    return null;
                case List<object> t1:
                    return t1.Cast<IDictionary<object, object>>().ToArray();
                case IDictionary<object, object> t2:
                    return new[] { t2 };
                default:
                    throw new Exception("Internal error: unexpected build-section contents");
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