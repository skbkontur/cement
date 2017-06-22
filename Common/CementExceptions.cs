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
        public BadYamlException(string moduleName, string sectionName, string message)
            : base($"Fail to parse {sectionName} section in {moduleName}/module.yaml: {message}")
        {
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
}
