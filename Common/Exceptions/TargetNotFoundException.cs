namespace Common.Exceptions
{
    public sealed class TargetNotFoundException : CementException
    {
        public TargetNotFoundException(string moduleName)
            : base($"Build target is not specified in {moduleName}/module.yaml")
        {
        }
    }
}
