namespace Common.Exceptions
{
    public sealed class NoSuchConfigurationException : CementException
    {
        public NoSuchConfigurationException(string moduleName, string missingConfiguration)
            : base($"Configuration {missingConfiguration} not found in {moduleName}")
        {
        }
    }
}
