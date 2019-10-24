using System.Net;
using Common.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                var infoModel = JsonConvert.DeserializeObject<InfoResponseModel>(webClient.DownloadString($"{CementSettings.Get().CementServer}/api/v1/cement/info/head/" + branch));
                return infoModel?.CommitHash;
            }
            catch (WebException ex)
            {
                log.LogError("Fail self-update ", ex);
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse) ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                    {
                        ConsoleWriter.WriteError("Failed to look for updates on branch " + branch + ". Server responsed 404");
                        return null;
                    }
                }
                ConsoleWriter.WriteError("Failed to look for updates on branch " + branch + ": " + ex.Message);
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