using System;
using Common.YamlParsers;
using System.Collections.Generic;
using System.Linq;
using Common.Extensions;

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
            var parameters = (buildSection.Parameters.Count == 0
                ? GetDefaultMsbuildParameters(buildSection.Tool)
                : buildSection.Parameters.Select(EscapeSemicolon)).ToList();
            parameters.Add("/p:Configuration=" + buildSection.Configuration);
            parameters.Add(buildSection.Target);

            var minMsBuildVersionWithRestoreTarget = new Version(15, 5, 180);

            if (tool.IsWindowsMsBuild && tool.Version >= minMsBuildVersionWithRestoreTarget)
                parameters.Add("/restore");

            var toolPath = tool.Path;

            if (!Helper.OsIsUnix())
                toolPath = "\"" + tool.Path + "\"";
            return toolPath + " " + string.Join(" ", parameters);
        }

        private static MsBuildLikeTool FindTool(Tool buildTool, string moduleName)
        {
            if (buildTool.Name != "msbuild")
                return new MsBuildLikeTool(buildTool.Name);
            if (Helper.OsIsUnix())
                return new MsBuildLikeTool("msbuild");

            var tool = ModuleBuilderHelper.FindMsBuild(buildTool.Version, moduleName);

            return tool;
        }

        private static readonly string[] DefaultMsbuildParameters =
        {
            @"/t:Rebuild",
            @"/nodeReuse:false",
            @"/maxcpucount",
            @"/v:m",
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

        private static string EscapeSemicolon(string cmdPart)
        {
            if (!Helper.OsIsUnix())
                return cmdPart;
            return cmdPart.Replace(";", "\\;");
        }
    }
}