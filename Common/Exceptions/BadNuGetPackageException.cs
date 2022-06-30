namespace Common
{
    public sealed class BadNuGetPackageException : CementException
    {
        public BadNuGetPackageException(string packageName)
            : base($"Wrong package declaration: {packageName}. Package must be in format packageId/version")
        {
        }
    }
}