using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class CommandSettings
{
    public bool MeasureElapsedTime { get; set; }
    public bool RequireModuleYaml { get; set; }
    public CommandLocation Location { get; set; }
}
