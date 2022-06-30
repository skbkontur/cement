namespace Common.Updaters
{
    public interface ICementUpdater
    {
        string Name { get; }

        string GetNewCommitHash();
        byte[] GetNewCementZip();
    }
}
