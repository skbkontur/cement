namespace Common
{
    public class InfoResponseModel
    {
        public string CommitHash { get; }
        
        public InfoResponseModel(string commitHash)
        {
            CommitHash = commitHash;
        }
    }
}