using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class SelfUpdateCommandOptions
{
    public SelfUpdateCommandOptions(string branch, string server)
    {
        Branch = branch;
        Server = server;
    }

    public string Branch { get; }
    public string Server { get; }
}