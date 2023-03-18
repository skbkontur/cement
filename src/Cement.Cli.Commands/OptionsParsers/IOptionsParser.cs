namespace Cement.Cli.Commands.OptionsParsers;

public interface IOptionsParser<out TOptions>
{
    TOptions Parse(string[] args);
}
