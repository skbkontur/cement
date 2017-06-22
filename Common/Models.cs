namespace Common
{
    public class InfoResponseModel
    {
        public string Package { get; }
        public string Branch { get; }
        public string CommitHash { get; }
        public string Created { get; }

        public InfoResponseModel(string package, string branch, string commitHash, string created)
        {
            Package = package;
            Branch = branch;
            CommitHash = commitHash;
            Created = created;
        }
    }
}
