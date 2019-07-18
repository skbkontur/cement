using JetBrains.Annotations;

namespace Common.Exceptions
{
    [PublicAPI]
    public sealed class GitSubmoduleAddException : GitSubmoduleException
    {
        public GitSubmoduleAddException(string message)
            : base(message)
        {
        }
    }
}