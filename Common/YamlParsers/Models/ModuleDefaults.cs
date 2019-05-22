using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ModuleDefaults
    {
        [CanBeNull]
        public string[] HooksSection { get; set; }

        [CanBeNull]
        public DepsContent DepsSection { get; set; }

        [CanBeNull]
        public ModuleSettings SettingsSection { get; set;}

        [CanBeNull]
        public BuildData BuildSection { get; set; }

        [CanBeNull]
        public InstallData InstallSection { get; set; }
    }
}