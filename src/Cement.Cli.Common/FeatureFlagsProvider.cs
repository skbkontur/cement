using System.IO;
using Microsoft.Extensions.Configuration;

namespace Cement.Cli.Common;

public sealed class FeatureFlagsProvider
{
    private readonly ConsoleWriter consoleWriter;

    public FeatureFlagsProvider(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public FeatureFlags Get()
    {
        var featureFlagsConfigPath = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "featureFlags.json");
        if (File.Exists(featureFlagsConfigPath))
        {
            var configuration = new ConfigurationBuilder().AddJsonFile(featureFlagsConfigPath).Build();
            return configuration.Get<FeatureFlags>();
        }

        consoleWriter.WriteWarning(
            $"File with feature flags not found in '{featureFlagsConfigPath}'. " +
            "Reinstalling cement may fix that issue");

        return FeatureFlags.Default;
    }
}
