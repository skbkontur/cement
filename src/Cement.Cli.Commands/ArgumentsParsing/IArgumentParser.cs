namespace Cement.Cli.Commands.ArgumentsParsing;

public interface IArgumentParser<out TOptions>
{
    TOptions Parse(string[] args);
}
