namespace Cement.Cli.Common.DepsValidators;

public interface IDepsValidatorFactory
{
    IDepsValidator Create(string configurationName);
}
