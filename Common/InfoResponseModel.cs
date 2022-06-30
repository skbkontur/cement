namespace Common
{
    public sealed class InfoResponseModel
    {
        public string CommitHash { get; }
        
        public InfoResponseModel(string commitHash)
        {
            CommitHash = commitHash;
        }
    }
}