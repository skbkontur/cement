using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Common;

public static class CementSettingsRepository
{
    public static void Save(CementSettings settings)
    {
        var text = JsonConvert.SerializeObject(settings, Formatting.Indented);
        var path = Path.Combine(Helper.GetGlobalCementDirectory(), "settings");
        Helper.CreateFileAndDirectory(path, text);
    }

    public static CementSettings Get()
    {
        var path = Path.Combine(Helper.GetGlobalCementDirectory(), "settings");
        var defaultSettings = GetDefaultSettings();

        var settings = ReadSettings(path);
        settings = settings ?? defaultSettings;

        settings.Packages = settings.Packages ?? defaultSettings.Packages;
        foreach (var package in defaultSettings.Packages)
        {
            if (settings.Packages.All(p => p.Name != package.Name))
                settings.Packages.Add(package);
        }

        settings.SelfUpdateTreeish = settings.SelfUpdateTreeish ?? defaultSettings.SelfUpdateTreeish;
        settings.UserCommands = settings.UserCommands ?? defaultSettings.UserCommands;
        settings.CementServer = settings.CementServer ?? defaultSettings.CementServer;

        if (settings.Password != null)
        {
            settings.EncryptedPassword = Helper.Encrypt(settings.Password);
            settings.Password = null;
        }

        return settings;
    }

    private static CementSettings GetDefaultSettings()
    {
        var path = Helper.GetCementDefaultSettingsPath();
        if (!File.Exists(path))
            ConsoleWriter.Shared.WriteError($"{path} not found");
        return ReadSettings(path);
    }

    private static CementSettings ReadSettings(string path)
    {
        if (!File.Exists(path))
            return new CementSettings();

        try
        {
            var data = File.ReadAllText(path);
            var settings = JsonConvert.DeserializeObject<CementSettings>(data);
            return settings ?? new CementSettings();
        }
        catch (Exception exception)
        {
            ConsoleWriter.Shared.WriteWarning("Can't read cement settings from " + path + ": " + exception.Message);
            return new CementSettings();
        }
    }
}
