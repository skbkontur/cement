using Common.YamlParsers;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class BuildYamlScriptsMaker
    {
        public static List<BuildScriptWithBuildData> PrepareBuildScriptsFromYaml(Dep dep)
        {
            var buildSections = Yaml.BuildParser(dep.Name).Get(dep.Configuration);

            var result = new List<BuildScriptWithBuildData>();
            foreach (var buildSection in buildSections)
            {
                if (buildSection.Target.IsFakeTarget())
                    result.Add(null);
                else
                {
                    var script = MakeScript(dep, buildSection);
                    result.Add(new BuildScriptWithBuildData(
                        script,
                        buildSection));
                }
            }
            return result;
        }

        private static string MakeScript(Dep dep, BuildData buildSection)
        {
            switch (buildSection.Tool.Name)
            {
                case "msbuild":
                case "dotnet":
                    return BuildMsbuildScript(buildSection, dep.Name);
                default:
                    return BuildShellScript(buildSection);
            }
        }

        private static string BuildShellScript(BuildData buildSection)
        {
            var res = buildSection.Tool.Name + " " +
                      string.Join(" ", buildSection.Parameters) + " " +
                      buildSection.Target;
            return res;
        }

        private static string BuildMsbuildScript(BuildData buildSection, string moduleName)
        {
            var tool = FindTool(buildSection.Tool, moduleName);
            var parameters = (buildSection.Parameters.Count == 0 ? GetDefaultMsbuildParameters(buildSection.Tool) : buildSection.Parameters).ToList();
            parameters.Add("/p:Configuration=" + buildSection.Configuration);
            parameters.Add(buildSection.Target);

            if (!Helper.OsIsUnix())
                tool = "\"" + tool + "\"";
            return tool + " " + string.Join(" ", parameters);
        }

        private static string FindTool(Tool buildTool, string moduleName)
        {
            if (buildTool.Name != "msbuild")
                return buildTool.Name;
            if (Helper.OsIsUnix())
                return "msbuild";

            return ModuleBuilderHelper.FindMsBuild(buildTool.Version, moduleName);
        }

        private static readonly string[] DefaultMsbuildParameters =
        {
            @"/t:Rebuild",
            @"/nodeReuse:false",
            @"/maxcpucount",
            @"/v:m"
        };

        private static readonly string[] DefaultXbuildParameters =
        {
            @"/t:Rebuild",
            @"/v:m"
        };

        private static readonly string[] DefaultDotnetParameters =
        {
            @"build"
        };

        private static List<string> GetDefaultMsbuildParameters(Tool tool)
        {
            var parameters = GetDefaultMsbuildParameters(tool.Name);
            var toolVersion = tool.Version;
            if (!Helper.OsIsUnix() && Helper.IsVisualStudioVersion(toolVersion))
                parameters.Add($"/p:VisualStudioVersion={toolVersion}");
            return parameters;
        }

        private static List<string> GetDefaultMsbuildParameters(string toolName)
        {
            if (toolName == "dotnet")
                return DefaultDotnetParameters.ToList();

            return (Helper.OsIsUnix() ? DefaultXbuildParameters : DefaultMsbuildParameters).ToList();
        }
    }
}
