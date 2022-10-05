namespace Common;

public sealed class GetInfo
{
    public bool Cloned;
    public string ForcedBranch;
    public bool Changed;
    public bool ForcedLocal;
    public bool Pulled;
    public bool Reset;
    public string CommitInfo;
    public bool HookUpdated;

    public GetInfo()
    {
        Cloned = false;
        ForcedBranch = null;
        Changed = false;
        ForcedLocal = false;
        Pulled = false;
        Reset = false;
        HookUpdated = false;
        CommitInfo = "";
    }

    public bool Forced => ForcedBranch != null;
}
