using System.Net;
using log4net;
using Newtonsoft.Json;

namespace Common
{
    public class CementFromServerUpdater : ICementUpdater
    {
        private readonly string server;
        private readonly string branch;
        private readonly ILog log;

        public CementFromServerUpdater(string server, string branch, ILog log)
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
                ConsoleWriter.WriteProgressWithoutSave("Looking for cement updates");
                var infoModel = JsonConvert.DeserializeObject<InfoResponseModel>(
                    webClient.DownloadString($"{CementSettings.Get().CementServer}/api/v1/cement/info/head/" + branch));
                return infoModel?.CommitHash;
            }
            catch (WebException ex)
            {
                log.Error("Fail self-update ", ex);
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse) ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                    {
                        ConsoleWriter.WriteError("Failed to look for updates on branch " + branch +
                                                 ". Server responsed 404");
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
    }
}
