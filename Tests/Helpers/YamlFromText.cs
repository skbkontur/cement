using System.IO;
using Common;
using Common.YamlParsers;

namespace Tests.Helpers
{
    public static class YamlFromText
    {
        public static ConfigurationYamlParser ConfigurationParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new ConfigurationYamlParser(new FileInfo(dir.Path));
            }
        }

        public static DepsYamlParser DepsParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new DepsYamlParser(new FileInfo(dir.Path));
            }
        }

        public static InstallYamlParser InstallParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new InstallYamlParser(new FileInfo(dir.Path));
            }
        }

        public static BuildYamlParser BuildParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new BuildYamlParser(new FileInfo(dir.Path));
            }
        }

        public static SettingsYamlParser SettingsParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new SettingsYamlParser(new FileInfo(dir.Path));
            }
        }

        public static HooksYamlParser HooksParser(string text)
        {
            using (var dir = new TempDirectory())
            {
                var yamlPath = Path.Combine(dir.Path, Helper.YamlSpecFile);
                File.WriteAllText(yamlPath, text);
                return new HooksYamlParser(new FileInfo(dir.Path));
            }
        }
    }
}
