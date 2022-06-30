namespace Common
{
    public sealed class DepWithCommitHash
    {
        public readonly Dep Dep;
        public readonly string CommitHash;

        public DepWithCommitHash(Dep dep, string commitHash)
        {
            Dep = dep;
            CommitHash = commitHash;
        }
    }
}