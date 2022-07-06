namespace Commands
{
    public interface ICommand
    {
        bool IsHiddenCommand { get; }
        string HelpMessage { get; }
        int Run(string[] args);
    }
}
