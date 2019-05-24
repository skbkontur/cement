using System;

namespace Common
{
    public class CementException : Exception
    {
        protected CementException()
        {
        }

        public CementException(string message)
            : base(message)
        {
        }
    }

    public class TargetNotFoundException : CementException
    {
        public TargetNotFoundException(string moduleName)
            : base($"Build target is not specified in {moduleName}/module.yaml")
        {
        }
    }

    public class CementTrackException : CementException
    {
        public CementTrackException(string message)
            : base(message)
        {
        }
    }

    public class CementBuildException : CementException
    {
        public CementBuildException(string message)
            : base(message)
        {
        }
    }

    public class NoSuchConfigurationException : CementException
    {
        public NoSuchConfigurationException(string moduleName, string missingConfiguration)
            : base($"Configuration {missingConfiguration} not found in {moduleName}")
        {
        }
    }

    public class BadYamlException : CementException
    {
        public string ModuleName { get; }
        public string SectionName { get; }

        public BadYamlException(string moduleName, string sectionName, string message)
            : base($"Fail to parse {sectionName} section in {moduleName}/module.yaml: {message}")
        {
            ModuleName = moduleName;
            SectionName = sectionName;
        }

        public BadYamlException(string sectionName, string message)
            : base($"Fail to parse {sectionName} section in module.yaml: {message}")
        {
            SectionName = sectionName;
        }
    }

    public class TreeishConflictException : CementException
    {
        public TreeishConflictException(string format)
            : base(format)
        {
        }
    }

    public class TimeoutException : CementException
    {
        public TimeoutException(string format)
            : base(format)
        {
        }
    }

    public class BadNuGetPackageException : CementException
    {
        public BadNuGetPackageException(string packageName)
            : base($"Wrong package declaration: {packageName}. Package must be in format packageId/version")
        {
        }
    }
}