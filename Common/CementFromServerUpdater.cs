using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Common
{
    public class CementFromServerUpdater : ICementUpdater
    {
        private readonly string server;
        private readonly string branch;
        private readonly ILogger log;

        public CementFromServerUpdater(string server, string branch, ILogger log)
        {
            this.server = server;
            this.branch = branch;
            this.log = log;
        }

        public string GetNewCommitHash()
        {
            var webClient = new WebClient();
            try
            {
                var info = JsonConvert.DeserializeObject<InfoResponseModel>(webClient.DownloadString($"{CementSettings.Get().CementServer}/api/v1/cement/info/head/" + branch));
                return info?.CommitHash;
            }
            catch (WebException webException)
            {
                log.LogError(webException,"Fail self-update, exception: '{ExceptionErrorMessage}'", webException.Message);
                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var response = (HttpWebResponse) webException.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                    {
                        ConsoleWriter.WriteWarning("Failed to look for updates on branch " + branch + ". Server replied 404");
                        return null;
                    }
                }
                ConsoleWriter.WriteWarning("Failed to look for updates on branch " + branch + ": " + webException.Message);
                return null;
            }
        }

        public byte[] GetNewCementZip()
        {
            var client = new WebClient();
            var zipContent = client.DownloadData($"{server}/api/v1/cement/head/{branch}");
            return zipContent;
        }

        public string GetName()
        {
            return server;
        }
    }
}