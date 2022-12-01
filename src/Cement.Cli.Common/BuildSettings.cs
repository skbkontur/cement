namespace Cement.Cli.Common;

public sealed class BuildSettings
{
    public bool ShowObsoleteWarnings { get; set; }

    public bool ShowAllWarnings { get; set; }

    public bool ShowOutput { get; set; }

    public bool ShowProgress { get; set; }

    public bool ShowWarningsSummary { get; set; }

    public bool CleanBeforeBuild { get; set; }
}
