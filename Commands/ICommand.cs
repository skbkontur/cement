namespace Commands
{
	public interface ICommand
	{
		int Run(string[] args);

	    bool IsHiddenCommand
	    {
	        get;
	    }
		string HelpMessage
		{
			get;
		}
	}
}