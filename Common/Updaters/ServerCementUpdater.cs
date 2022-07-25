using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Common.Extensions;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Updaters
{
    [PublicAPI]
    public sealed class ServerCementUpdater : ICementUpdater
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private readonly ILogger log;
        private readonly string branch;
        private readonly ConsoleWriter consoleWriter;

        public ServerCementUpdater(ILogger log, ConsoleWriter consoleWriter, string server, string branch)
        {
            Name = server;
            this.branch = branch;
            this.log = log;
            this.consoleWriter = consoleWriter;
        }

        public string Name { get; }

        public string GetNewCommitHash()
        {
            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);

                using var handler = new SocketsHttpHandler();
                using var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(Name)
                };

                var uri = new Uri($"api/v1/cement/info/head/{branch}", UriKind.Relative);
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Headers =
                    {
                        Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")},
                        UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                    }
                };

                using var httpResponseMessage = httpClient.Send(httpRequestMessage, cts.Token);
                httpResponseMessage.EnsureSuccessStatusCode();

                using var stream = httpResponseMessage.Content.ReadAsStream(cts.Token);
                using var streamReader = new StreamReader(stream, Encoding.UTF8);

                var content = streamReader.ReadToEnd();
                var info = JsonConvert.DeserializeObject<InfoResponseModel>(content);

                return info?.CommitHash;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to look for updates on server");
                consoleWriter.WriteWarning("Failed to look for updates on server: " + ex.Message);

                return null;
            }
        }

        public byte[] GetNewCementZip()
        {
            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);

                using var handler = new SocketsHttpHandler();
                using var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(Name)
                };

                var uri = new Uri($"api/v1/cement/head/{branch}", UriKind.Relative);
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Headers =
                    {
                        Accept = {MediaTypeWithQualityHeaderValue.Parse("application/octet-stream")},
                        UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                    }
                };

                using var httpResponseMessage = httpClient.Send(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                httpResponseMessage.EnsureSuccessStatusCode();

                using var stream = httpResponseMessage.Content.ReadAsStream(cts.Token);

                return stream.ReadAllBytesWithProgress(ReportProgress);

                void ReportProgress(long totalBytes)
                {
                    var readableBytes = totalBytes.Bytes().ToString();
                    consoleWriter.WriteProgress($"Downloading: {readableBytes}");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to look for updates on server, channel='{CementServerReleaseChannel}'", branch);
                consoleWriter.WriteWarning("Failed to look for updates on server: " + ex.Message);

                return null;
            }
        }
    }
}
