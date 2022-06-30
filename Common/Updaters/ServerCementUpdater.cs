using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Common.Updaters
{
    [PublicAPI]
    public sealed class ServerCementUpdater : ICementUpdater
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ILogger log;
        private readonly string server;
        private readonly string branch;

        public ServerCementUpdater(ILogger log, string server, string branch)
        {
            this.server = server;
            this.branch = branch;
            this.log = log;
        }

        public string GetNewCommitHash()
        {
            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);

                using var handler = new SocketsHttpHandler();
                using var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(server)
                };

                var uri = new Uri($"/api/v1/cement/info/head/{branch}", UriKind.Relative);
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
                ConsoleWriter.Shared.WriteWarning("Failed to look for updates on server: " + ex.Message);

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
                    BaseAddress = new Uri(server)
                };

                var uri = new Uri($"/api/v1/cement/head/{branch}", UriKind.Relative);
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Headers =
                    {
                        Accept = {MediaTypeWithQualityHeaderValue.Parse("application/octet-stream")},
                        UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                    }
                };

                using var httpResponseMessage = httpClient.Send(httpRequestMessage, cts.Token);
                httpResponseMessage.EnsureSuccessStatusCode();

                using var stream = httpResponseMessage.Content.ReadAsStream(cts.Token);
                return ReadAllBytes(stream);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to look for updates on server, channel='{CementServerReleaseChannel}'", branch);
                ConsoleWriter.Shared.WriteWarning("Failed to look for updates on server: " + ex.Message);

                return null;
            }
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            return buffer.ToArray();
        }

        public string Name => server;
    }
}
