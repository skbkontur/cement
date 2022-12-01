using System;
using Cement.Cli.Common;
using Cement.Cli.Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public static class SelfUpdate
{
    public static void UpdateIfOld(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
    {
        var log = LogManager.GetLogger(nameof(SelfUpdate));

        try
        {
            var isEnabledSelfUpdate = CementSettingsRepository.Get().IsEnabledSelfUpdate;
            if (isEnabledSelfUpdate.HasValue && !isEnabledSelfUpdate.Value)
                return;
            var lastUpdate = Helper.GetLastUpdateTime();
            var now = DateTime.Now;
            var diff = now - lastUpdate;
            if (diff <= TimeSpan.FromHours(5))
                return;

            var logger = LogManager.GetLogger<SelfUpdateCommand>();
            var selfUpdateCommand = new SelfUpdateCommand(logger, consoleWriter, featureFlags)
            {
                IsAutoUpdate = true
            };

            var exitCode = selfUpdateCommand.Run(new[] {"self-update"});
            if (exitCode != 0)
            {
                log.LogError("Auto update cement failed. 'self-update' exited with code '{Code}'", exitCode);
                consoleWriter.WriteWarning("Auto update failed. Check previous warnings for details");
            }
        }
        catch (Exception exception)
        {
            log.LogError(exception, "Auto update failed, error: '{ErrorMessage}'", exception.Message);
            consoleWriter.WriteWarning("Auto update failed. Check logs for details");
        }
    }
}
