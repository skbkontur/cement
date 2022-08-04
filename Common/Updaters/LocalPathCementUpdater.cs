using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Common.Updaters
{
    public sealed class LocalPathCementUpdater : ICementUpdater
    {
        private readonly ILogger log;

        public LocalPathCementUpdater(ILogger log)
        {
            this.log = log;
        }

        public string Name => "fileSystemLocalPath";

        public string GetNewCommitHash()
        {
            return DateTime.Now.Ticks.ToString();
        }

        public byte[] GetNewCementZip()
        {
            try
            {
                return File.ReadAllBytes(Path.Combine(GetZipCementDirectory(), "cement.zip"));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Fail self-update, exception: '{ErrorMessage}'", ex.Message);
            }

            return null;
        }

        public void Dispose()
        {
        }

        private static string GetZipCementDirectory()
        {
            var zipDir = Path.Combine(Helper.HomeDirectory(), "work");

            if (!Directory.Exists(zipDir))
                Directory.CreateDirectory(zipDir);

            return zipDir;
        }
    }
}
