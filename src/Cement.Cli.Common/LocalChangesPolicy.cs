namespace Cement.Cli.Common;

public enum LocalChangesPolicy
{
    FailOnLocalChanges,
    Reset,
    ForceLocal,
    Pull,
    Interactive
}
