using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.Extensions;

namespace Cement.Cli.Common.YamlParsers;

public static class Yaml
{
    public static bool Exists(string moduleName)
    {
        return File.Exists(Path.Combine(Helper.CurrentWorkspace, moduleName, Helper.YamlSpecFile));
    }

    public static string ReadAllText(string moduleName)
    {
        return File.ReadAllText(Path.Combine(Helper.CurrentWorkspace, moduleName, Helper.YamlSpecFile));
    }

    public static ConfigurationYamlParser ConfigurationParser(string moduleName)
    {
        return new ConfigurationYamlParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static DepsYamlParser DepsParser(string moduleName)
    {
        return new DepsYamlParser(ConsoleWriter.Shared, DepsValidatorFactory.Shared,
            new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static InstallYamlParser InstallParser(string moduleName)
    {
        return new InstallYamlParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static BuildYamlParser BuildParser(string moduleName)
    {
        return new BuildYamlParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static SettingsYamlParser SettingsParser(string moduleName)
    {
        return new SettingsYamlParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static HooksYamlParser HooksParser(string moduleName)
    {
        return new HooksYamlParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
    }

    public static List<string> GetCsprojsList(string moduleName)
    {
        if (!Exists(moduleName))
            return new List<string>();

        var configs = ConfigurationParser(moduleName).GetConfigurations();
        var buildsInfo = configs.SelectMany(config => BuildParser(moduleName).Get(config));
        var files = new List<string>();
        var moduleDirectory = Path.Combine(Helper.CurrentWorkspace, moduleName);

        var projects = buildsInfo.Select(info => info.Target)
            .Where(t => !t.IsFakeTarget()).Distinct();

        foreach (var project in projects)
        {
            var vsParser = new VisualStudioProjectParser(
                Path.Combine(moduleDirectory, project), Helper.GetModules());
            files.AddRange(vsParser.GetCsprojList());
        }

        return files.Distinct().ToList();
    }

    public static string GetProjectFileName(string project, string moduleName)
    {
        if (File.Exists(project))
        {
            return project;
        }

        var all = GetCsprojsList(moduleName);
        var projectNameFromSln = all.FirstOrDefault(
            f =>
                string.Equals(Path.GetFileName(f), project, StringComparison.CurrentCultureIgnoreCase));
        if (projectNameFromSln != null && File.Exists(projectNameFromSln))
            return projectNameFromSln;

        throw new CementException($"Project file '{project}' does not exist.");
    }

    public static List<string> GetSolutionList(string moduleName)
    {
        if (!Exists(moduleName))
            return new List<string>();

        var configs = ConfigurationParser(moduleName).GetConfigurations();
        var buildsInfo = configs.SelectMany(config => BuildParser(moduleName).Get(config));
        var moduleDirectory = Path.Combine(Helper.CurrentWorkspace, moduleName);
        var solutions = buildsInfo
            .Select(info => info.Target)
            .Where(target => !target.IsFakeTarget())
            .Where(target => target.EndsWith(".sln"))
            .Select(solutionRelativePath => Path.Combine(moduleDirectory, solutionRelativePath))
            .Distinct()
            .ToList();
        return solutions;
    }
}
