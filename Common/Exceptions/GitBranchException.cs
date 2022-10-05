﻿namespace Common.Exceptions;

public sealed class GitBranchException : CementException
{
    public GitBranchException(string message)
        : base(message)
    {
    }
}
