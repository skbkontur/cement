using JetBrains.Annotations;

namespace Common.Exceptions
{
    [PublicAPI]
    public sealed class GitSubmoduleUpdateException : GitSubmoduleException
    {
        public GitSubmoduleUpdateException(string message)
            : base(message)
        {
        }
    }
}