using JetBrains.Annotations;

namespace Common.Exceptions
{
    [PublicAPI]
    public sealed class GitSubmoduleInitException : GitSubmoduleException
    {
        public GitSubmoduleInitException(string message)
            : base(message)
        {
        }
    }
}