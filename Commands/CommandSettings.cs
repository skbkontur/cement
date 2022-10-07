using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class CommandSettings
{
    public string LogFileName { get; set; }
    public bool MeasureElapsedTime { get; set; }
    public bool RequireModuleYaml { get; set; }
    public bool NoElkLog { get; set; }
    public CommandLocation Location { get; set; }
}
