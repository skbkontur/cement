namespace Common.Exceptions;

public sealed class BadYamlException : CementException
{
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

    public string ModuleName { get; }
    public string SectionName { get; }
}
