namespace Common
{
    public interface ICementUpdater
    {
        string GetNewCommitHash();
        byte[] GetNewCementZip();
        string GetName();
    }
}