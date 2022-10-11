using System;
using System.IO;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Common;

[PublicAPI]
public sealed class NuGetHelper
{
    private readonly ILogger<NuGetHelper> logger;
    private readonly ConsoleWriter consoleWriter;

    private NuGetHelper(ILogger<NuGetHelper> logger, ConsoleWriter consoleWriter)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
    }

    public static NuGetHelper Shared { get; } = new(LogManager.GetLogger<NuGetHelper>(), ConsoleWriter.Shared);

    public string GetNugetPackageVersion(string packageName, string nugetRunCommand, bool preRelease)
    {
        consoleWriter.WriteProgressWithoutSave("Get package version for " + packageName);

        var command = $"{nugetRunCommand} list {packageName} -NonInteractive" + (preRelease ? " -PreRelease" : "");

        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        // todo(dstarasov): не проверяется exitCode
        var (_, output, errors) = shellRunner.Run(command);

        foreach (var line in output.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
        {
            var lineTokens = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (lineTokens.Length == 2 && lineTokens[0].Equals(packageName, StringComparison.InvariantCultureIgnoreCase))
            {
                var msg = $"Got package version: {lineTokens[1]} for {packageName}";
                logger.LogInformation(msg);
                consoleWriter.WriteInfo(msg);
                return lineTokens[1];
            }
        }

        var message = $"not found package version for package {packageName}. nuget output: " + output + errors;
        logger.LogDebug(message);
        consoleWriter.WriteWarning(message);
        return null;
    }

    public string GetNugetRunCommand()
    {
        var nuGetPath = GetNuGetPath();
        if (nuGetPath == null)
            return null;
        var nugetCommand = $"\"{nuGetPath}\"";
        if (Platform.IsUnix())
            nugetCommand = $"mono {nugetCommand}";
        return nugetCommand;
    }

    private string GetNuGetPath()
    {
        var nuget = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "NuGet.exe");
        if (!File.Exists(nuget))
        {
            logger.LogError($"NuGet.exe not found in {nuget}");
            return null;
        }

        logger.LogDebug($"NuGet.exe found in {nuget}");
        return nuget;
    }
}
