namespace Common
{
    public sealed class InfoResponseModel
    {
        public InfoResponseModel(string commitHash)
        {
            CommitHash = commitHash;
        }

        public string CommitHash { get; }
    }
}
