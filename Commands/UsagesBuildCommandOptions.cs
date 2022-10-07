using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class UsagesBuildCommandOptions
{
    public UsagesBuildCommandOptions(bool pause, string checkingBranch)
    {
        Pause = pause;
        CheckingBranch = checkingBranch;
    }

    public bool Pause { get; }

    public string CheckingBranch { get; }
}