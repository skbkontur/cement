namespace Cement.Cli.Common;

public sealed class ReferenceWithCsproj
{
    public readonly string CsprojFile;
    public readonly string Reference;

    public ReferenceWithCsproj(string reference, string csprojFile)
    {
        Reference = reference;
        CsprojFile = csprojFile;
    }
}
