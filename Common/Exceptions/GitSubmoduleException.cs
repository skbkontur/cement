using JetBrains.Annotations;

namespace Common.Exceptions
{
    [PublicAPI]
    public abstract class GitSubmoduleException : CementException
    {
        protected GitSubmoduleException()
        {
        }

        protected GitSubmoduleException(string message)
            : base(message)
        {
        }
    }
}