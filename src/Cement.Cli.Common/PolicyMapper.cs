namespace Cement.Cli.Common;

public static class PolicyMapper
{
    public static LocalChangesPolicy GetLocalChangesPolicy(bool force, bool reset, bool pullAnyway)
    {
        if (force)
            return LocalChangesPolicy.ForceLocal;

        if (reset)
            return LocalChangesPolicy.Reset;

        if (pullAnyway)
            return LocalChangesPolicy.Pull;

        return LocalChangesPolicy.FailOnLocalChanges;
    }
}
