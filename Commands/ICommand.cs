﻿namespace Commands
{
    public interface ICommand
    {
        string Name { get; }

        bool IsHiddenCommand { get; }

        string HelpMessage { get; }

        int Run(string[] args);
    }
}
