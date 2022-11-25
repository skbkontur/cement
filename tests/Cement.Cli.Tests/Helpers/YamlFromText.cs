﻿using System.IO;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.YamlParsers;

namespace Cement.Cli.Tests.Helpers;

public static class YamlFromText
{
    public static ConfigurationYamlParser ConfigurationParser(string text)
    {
        const string fakeModuleName = "some_module";
        return new ConfigurationYamlParser(fakeModuleName, text);
    }

    public static DepsYamlParser DepsParser(string text)
    {
        using var dir = new TempDirectory();
        var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
        File.WriteAllText(yamlPath, text);
        return new DepsYamlParser(ConsoleWriter.Shared, DepsValidatorFactory.Shared, new FileInfo(dir.Path));
    }

    public static InstallYamlParser InstallParser(string text)
    {
        const string fakeModuleName = "some_module";
        return new InstallYamlParser(fakeModuleName, text);
    }

    public static BuildYamlParser BuildParser(string text)
    {
        using var dir = new TempDirectory();
        var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
        File.WriteAllText(yamlPath, text);
        return new BuildYamlParser(new FileInfo(dir.Path));
    }

    public static SettingsYamlParser SettingsParser(string text)
    {
        using var dir = new TempDirectory();
        var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
        File.WriteAllText(yamlPath, text);
        return new SettingsYamlParser(new FileInfo(dir.Path));
    }

    public static HooksYamlParser HooksParser(string text)
    {
        const string fakeModuleName = "some_module";
        return new HooksYamlParser(fakeModuleName, text);
    }
}
