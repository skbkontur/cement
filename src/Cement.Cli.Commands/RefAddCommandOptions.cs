using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RefAddCommandOptions
{
    public RefAddCommandOptions(string project, Dep dep, bool testReplaces, bool force)
    {
        Project = project;
        Dep = dep;
        TestReplaces = testReplaces;
        Force = force;
    }

    public string Project { get; }
    public Dep Dep { get; }
    public bool TestReplaces { get; }
    public bool Force { get; }
}