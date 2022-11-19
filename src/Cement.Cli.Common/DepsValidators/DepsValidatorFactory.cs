namespace Cement.Cli.Common.DepsValidators;

public sealed class DepsValidatorFactory : IDepsValidatorFactory
{
    public static DepsValidatorFactory Shared { get; } = new();

    public IDepsValidator Create(string configurationName)
    {
        return new DepsValidator(configurationName);
    }
}
