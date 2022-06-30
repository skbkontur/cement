namespace Common
{
    public sealed class DepWithParent
    {
        public Dep Dep { get; }
        public string ParentModule { get; }

        public DepWithParent(Dep dep, string parentModule)
        {
            Dep = dep;
            ParentModule = parentModule;
        }
    }
}