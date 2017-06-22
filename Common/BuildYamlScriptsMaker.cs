using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers;

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
                if (buildSection.Target == null || buildSection.Target.Equals("None"))
                    result.Add(null);
                else
                {
                    var script = buildSection.Tool.Name == "msbuild" ? BuildMsbuildScript(buildSection, dep.Name) : BuildShellScript(buildSection);
                    var scriptIfFail = buildSection.Tool.Name == "msbuild" ? BuildMsbuildScript(buildSection, dep.Name) : BuildShellScript(buildSection);
                    result.Add(new BuildScriptWithBuildData(
                        script,
                        scriptIfFail,
                        buildSection));
                }
            }
            return result;
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
            var parameters = (buildSection.Parameters.Count == 0 ? GetDefaultMsbuildParameters(buildSection.Tool.Version) : buildSection.Parameters).ToList();
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
                return "xbuild";

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

        private static List<string> GetDefaultMsbuildParameters(string toolVersion)
        {
            var parameters = (Helper.OsIsUnix() ? DefaultXbuildParameters : DefaultMsbuildParameters).ToList();
            if (!Helper.OsIsUnix() && Helper.IsVisualStudioVersion(toolVersion))
                parameters.Add($"/p:VisualStudioVersion={toolVersion}");
            return parameters;
        }
    }
}