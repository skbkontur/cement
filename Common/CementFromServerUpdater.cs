using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

        public async Task<string> GetNewCommitHashAsync()
        {
            try
            {
                var client = new HttpClient();
                using (HttpResponseMessage response = await client.GetAsync($"{CementSettings.Get().CementServer}/api/v1/cement/info/head/" + branch))
                {
                    using (HttpContent content = response.Content)
                    {
                        var jsonContent = await content.ReadAsStringAsync();
                        var info =  JsonConvert.DeserializeObject<InfoResponseModel>(jsonContent);
                        return info?.CommitHash;
                    }
                }
            }
            catch (WebException webException)
            {
                log.LogError(webException, "Fail self-update, exception: '{ExceptionMessage}'", webException.Message);
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

        public async Task<byte[]> GetNewCementZipAsync()
        {
            var client = new HttpClient();
            using (HttpResponseMessage response = await client.GetAsync($"{server}/api/v1/cement/head/{branch}"))
            {
                using (HttpContent content = response.Content)
                {
                    var zipContent = await content.ReadAsByteArrayAsync();
                    return zipContent;
                }
            }
        }

        public string GetName()
        {
            return server;
        }
    }
}