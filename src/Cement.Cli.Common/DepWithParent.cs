namespace Cement.Cli.Common;

public sealed class DepWithParent
{
    public DepWithParent(Dep dep, string parentModule)
    {
        Dep = dep;
        ParentModule = parentModule;
    }

    public Dep Dep { get; }
    public string ParentModule { get; }
}
