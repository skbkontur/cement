namespace Common.DepsValidators;

public sealed class DepsValidatorFactory : IDepsValidatorFactory
{
    public IDepsValidator Create(string configurationName)
    {
        return new DepsValidator(configurationName);
    }
}
