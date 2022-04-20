using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class CementFromGitHubUpdater : ICementUpdater
    {
        private readonly ILogger log;
        private string downloadUri;

        public CementFromGitHubUpdater(ILogger log)
        {
            this.log = log;
        }

        public async Task<string> GetNewCommitHashAsync()
        {
            try
            {
                using (var webClient = new WebClientWithTimeout())
                {
                    webClient.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    var jsonResult = await webClient.GetStringAsync("https://api.github.com/repos/skbkontur/cement/releases/latest");
                    var release = JsonConvert.DeserializeObject<GitHubRelease>(jsonResult);
                    if (release.Assets.Count != 1)
                        throw new CementException("Failed to parse json:\n" + jsonResult);
                    downloadUri = release.Assets[0].BrowserDownloadUrl;

                    var parts = downloadUri.Split('/', '.');
                    return parts[parts.Length - 2];
                }
            }
            catch (WebException webException)
            {
                log.LogError(webException, "Fail self-update, exception: '{ErrorMessage}' ", webException.Message);
                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var response = (HttpWebResponse) webException.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                    {
                        ConsoleWriter.WriteWarning("Failed to look for updates on github. Server replied 404");
                        return null;
                    }
                }
                ConsoleWriter.WriteWarning("Failed to look for updates on github: " + webException.Message);
                return null;
            }
        }

        public async Task<byte[]> GetNewCementZipAsync()
        {
            using (var client = new WebClientWithTimeout())
            {
                var zipContent = await client.GetByteArrayAsync(downloadUri);
                return zipContent;
            }
        }

        public string GetName()
        {
            return "GitHub";
        }
    }

    public class GitHubRelease
    {
        public List<GitHubAsset> Assets;
    }

    public class GitHubAsset
    {
        [JsonProperty(PropertyName = "browser_download_url")] public string BrowserDownloadUrl;
    }
}