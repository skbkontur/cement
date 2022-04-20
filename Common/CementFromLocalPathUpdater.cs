using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Common;

public sealed class CementFromLocalPathUpdater: ICementUpdater
{
    private readonly ILogger log;
    
    public CementFromLocalPathUpdater(ILogger log)
    {
        this.log = log;
    }
    
    public string GetNewCommitHash()
    {
        return DateTime.Now.Ticks.ToString();
    }

    public byte[] GetNewCementZip()
    {
        try
        {
            return File.ReadAllBytes(Path.Combine(Helper.GetZipCementDirectory(), "cement.zip"));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Fail self-update, exception: '{ErrorMessage}'", ex.Message);
        }
        return null;
    }

    public string GetName() =>
        "fileSystemLocalPath" ;
}