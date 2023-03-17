namespace Cement.Cli.Commands.ArgumentsParsing;

public interface IOptionsParser<out TOptions>
{
    TOptions Parse(string[] args);
}
