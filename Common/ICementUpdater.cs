using System.Threading.Tasks;

namespace Common
{
    public interface ICementUpdater
    {
        Task<string> GetNewCommitHashAsync();
        Task<byte[]> GetNewCementZipAsync();
        string GetName();
    }
}