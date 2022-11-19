using System;

namespace Cement.Cli.Common.Exceptions;

public class CementException : Exception
{
    public CementException(string message)
        : base(message)
    {
    }

    protected CementException()
    {
    }
}
