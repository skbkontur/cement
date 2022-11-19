using System.IO;
using Cement.Cli.Common;
using NUnit.Framework;

namespace Cement.Cli.Tests;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        EnsureDefaultSettingsExist();
    }

    private void EnsureDefaultSettingsExist()
    {
        var defaultSettingsPath = Helper.GetCementDefaultSettingsPath();
        if (File.Exists(defaultSettingsPath))
            return;

        var defaultSettingsFolder = Path.GetDirectoryName(defaultSettingsPath)!;
        if (!Directory.Exists(defaultSettingsFolder))
            Directory.CreateDirectory(defaultSettingsFolder);

        File.Copy("./files-common/defaultSettings.json", defaultSettingsPath);
    }
}
