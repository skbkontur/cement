using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Common;

public class CementFromLocalPathUpdater: ICementUpdater
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
        //var zipContent = File.ReadAllBytes(zipCementPath);
        try
        {

            using (FileStream fsSource = new FileStream(Path.Combine(Helper.GetZipCementDirectory(), "cement.zip"),
                       FileMode.Open, FileAccess.Read))
            {
                byte[] zipContent = new byte[fsSource.Length];
                int numBytesToRead = (int)fsSource.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int n = fsSource.Read(zipContent, numBytesRead, numBytesToRead);
                    
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                return zipContent;
            }
        }
        catch (Exception ex)
        {
            log.LogError($"Fail self-update, exception: '{ex}' ");
        }
        return null;
    }

    public string GetName() =>
        "fileSystemLocalPath" ;
}