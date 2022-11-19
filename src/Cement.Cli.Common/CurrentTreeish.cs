namespace Cement.Cli.Common;

public sealed class CurrentTreeish
{
    public readonly string Value;
    public readonly TreeishType Type;

    public CurrentTreeish(TreeishType type, string value)
    {
        Value = value;
        Type = type;
    }
}
