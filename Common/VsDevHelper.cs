using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Common;

[PublicAPI]
public sealed class VsDevHelper
{
    private readonly ILogger<VsDevHelper> logger;

    public VsDevHelper(ILogger<VsDevHelper> logger)
    {
        this.logger = logger;
    }

    public static Dictionary<string, string> GetCurrentSetVariables()
    {
        var result = new Dictionary<string, string>();

        var environmentVariables = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry de in environmentVariables)
        {
            result.Add(de.Key.ToString(), de.Value.ToString());
        }

        return result;
    }

    public void ReplaceVariablesToVs()
    {
        var variables = GetVsSetVariables();
        if (variables == null)
            return;
        foreach (var variable in variables)
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        logger.LogDebug("Successfully set new variables from VsDevCmd.bat");
    }

    private Dictionary<string, string> GetVsSetVariables()
    {
        var text = RunVsDevCmd();

        if (text == null)
            return null;

        var lines = text.Split('\n');
        var result = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            var equal = line.IndexOf("=", StringComparison.Ordinal);
            if (equal < 0)
                continue;
            var name = line.Substring(0, equal);
            var value = line.Substring(equal + 1);
            result.Add(name, value);
        }

        return result;
    }

    private string RunVsDevCmd()
    {
        var path = FindVsDevCmd();
        if (path == null)
        {
            logger.LogDebug("VsDevCmd.bat not found");
            return null;
        }

        logger.LogInformation($"VsDevCmd found in '{path}'");
        var command = $"\"{path}\" && set";
        var runner = new ShellRunner();
        if (runner.Run(command) != 0)
        {
            logger.LogDebug("VsDevCmd.bat not working");
            return null;
        }

        return runner.Output;
    }

    private static string FindVsDevCmd()
    {
        var paths = new List<KeyValuePair<string, string>>();
        var set = GetCurrentSetVariables();
        foreach (var key in set.Keys)
        {
            if (key.StartsWith("VS") && key.EndsWith("COMNTOOLS"))
                paths.Add(
                    new KeyValuePair<string, string>(
                        key,
                        Path.Combine(set[key], "VsDevCmd.bat")));
        }

        var programFiles = Helper.GetProgramFilesInfo();
        if (programFiles == null)
            return null;

        foreach (var version in Helper.VisualStudioVersions)
        foreach (var edition in Helper.VisualStudioEditions)
        {
            if (programFiles.x64 != null)
                paths.Add(
                    new KeyValuePair<string, string>(
                        Helper.GetEnvVariableByVisualStudioVersion(version),
                        Path.Combine(programFiles.x64, "Microsoft Visual Studio", version, edition, "Common7", "Tools", "VsDevCmd.bat")));

            if (programFiles.x86 != null)
                paths.Add(
                    new KeyValuePair<string, string>(
                        Helper.GetEnvVariableByVisualStudioVersion(version),
                        Path.Combine(programFiles.x86, "Microsoft Visual Studio", version, edition, "Common7", "Tools", "VsDevCmd.bat")));
        }

        paths = paths.OrderByDescending(x => x.Key).Where(x => File.Exists(x.Value)).ToList();
        if (!paths.Any())
            return null;
        return paths.FirstOrDefault().Value;
    }
}
