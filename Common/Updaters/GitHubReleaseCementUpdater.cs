using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Updaters
{
    [PublicAPI]
    public sealed class GitHubReleaseCementUpdater : ICementUpdater
    {
        private const string Owner = "skbkontur";
        private const string Repository = "cement";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private readonly ILogger log;

        public GitHubReleaseCementUpdater(ILogger log)
        {
            this.log = log;
        }

        public string Name => "GitHub";

        public string GetNewCommitHash()
        {
            try
            {
                var gitHubRelease = LoadGitHubRelease();
                if (gitHubRelease.Assets.Count == 1)
                    return gitHubRelease.TargetCommitsh;

                log.LogError(
                    "The GitHub Release '{GitHubReleaseVersion}' has incorrect number of assets: {GitHubReleaseAssetsCount}\n" +
                    "{GitHubReleaseDetails}", gitHubRelease.Name, gitHubRelease.Assets.Count, gitHubRelease);

                throw new CementException($"The GitHub Release '{gitHubRelease.Name}' is invalid");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to look for updates on github");
                ConsoleWriter.Shared.WriteWarning("Failed to look for updates on github: " + ex.Message);

                return null;
            }
        }

        public byte[] GetNewCementZip()
        {
            try
            {
                var gitHubRelease = LoadGitHubRelease();
                var asset = gitHubRelease.Assets[0];

                return LoadGitHubReleaseAsset(asset);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to look for updates on github");
                ConsoleWriter.Shared.WriteWarning("Failed to look for updates on github: " + ex.Message);

                return null;
            }
        }

        private static GitHubRelease LoadGitHubRelease()
        {
            using var cts = new CancellationTokenSource(DefaultTimeout);

            using var handler = new SocketsHttpHandler();
            using var httpClient = new HttpClient(handler);

            var uri = new Uri($"https://api.github.com/repos/{Owner}/{Repository}/releases/latest");
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            {
                Headers =
                {
                    Accept = {MediaTypeWithQualityHeaderValue.Parse("application/vnd.github.v3+json")},
                    UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                }
            };

            using var httpResponseMessage = httpClient.Send(httpRequestMessage, cts.Token);
            httpResponseMessage.EnsureSuccessStatusCode();

            using var stream = httpResponseMessage.Content.ReadAsStream(cts.Token);
            using var streamReader = new StreamReader(stream, Encoding.UTF8);

            var content = streamReader.ReadToEnd();
            var gitHubRelease = JsonConvert.DeserializeObject<GitHubRelease>(content);

            return gitHubRelease;
        }

        private static byte[] LoadGitHubReleaseAsset(GitHubAsset gitHubAsset)
        {
            using var cts = new CancellationTokenSource(DefaultTimeout);

            using var handler = new SocketsHttpHandler();
            using var httpClient = new HttpClient(handler);

            var uri = new Uri(gitHubAsset.BrowserDownloadUrl);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            {
                Headers =
                {
                    Accept = {MediaTypeWithQualityHeaderValue.Parse(gitHubAsset.ContentType)},
                    UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                }
            };

            using var httpResponseMessage = httpClient.Send(httpRequestMessage, cts.Token);
            httpResponseMessage.EnsureSuccessStatusCode();

            using var stream = httpResponseMessage.Content.ReadAsStream(cts.Token);
            return stream.ReadAllBytes();
        }
    }
}
