using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ModuleDefaults
    {
        public ModuleDefaults()
        {
            HooksSection = new string[0];
            SettingsSection = new ModuleSettings();
        }

        [NotNull]
        public string[] HooksSection { get; set; }

        [NotNull]
        public ModuleSettings SettingsSection { get; set;}

        [CanBeNull]
        public DepsSection DepsSection { get; set; }

        [CanBeNull]
        public BuildData BuildSection { get; set; }

        [CanBeNull]
        public InstallData InstallSection { get; set; }
    }
}