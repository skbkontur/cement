using JetBrains.Annotations;

namespace Cement.Cli.Common;

[PublicAPI]
public sealed class GetInfo
{
    public bool Cloned { get; set; }

    public string ForcedBranch { get; set; }

    public bool Changed { get; set; }

    public bool ForcedLocal { get; set; }

    public bool Pulled { get; set; }

    public bool Reset { get; set; }

    public string CommitInfo { get; set; } = "";

    public bool HookUpdated { get; set; }

    public bool Forced => ForcedBranch != null;
}
