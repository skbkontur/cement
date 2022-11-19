using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Cement.Cli.Common.Extensions;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cement.Cli.Common.Updaters;

[PublicAPI]
public sealed class ServerCementUpdater : ICementUpdater
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger log;
    private readonly string branch;
    private readonly ConsoleWriter consoleWriter;
    private readonly HttpClient httpClient;

    public ServerCementUpdater(ILogger log, ConsoleWriter consoleWriter, string server, string branch)
    {
        Name = server;
        httpClient = new HttpClient();
        this.branch = branch;
        this.log = log;
        this.consoleWriter = consoleWriter;
    }

    public string Name { get; private set; }

    public string GetNewCommitHash()
    {
        try
        {
            using var cts = new CancellationTokenSource(DefaultTimeout);

            var requestUri = new Uri(new Uri(Name), $"api/v1/cement/info/head/{branch}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")},
                    UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                }
            };

            using var response = httpClient.Send(request, cts.Token);
            response.EnsureSuccessStatusCode();

            // dstarasov: при редиректе свойство RequestUri в запросе изменится на новый адрес
            UpdateCementServerIfRequestWasRedirected(requestUri, request.RequestUri);

            using var contentStream = response.Content.ReadAsStream(cts.Token);
            using var contentStreamReader = new StreamReader(contentStream, Encoding.UTF8);

            var content = contentStreamReader.ReadToEnd();
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

            var requestUri = new Uri(new Uri(Name), $"api/v1/cement/head/{branch}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    Accept = {MediaTypeWithQualityHeaderValue.Parse("application/octet-stream")},
                    UserAgent = {ProductInfoHeaderValue.Parse("Anything")}
                }
            };

            using var response = httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            // dstarasov: при редиректе свойство RequestUri в запросе изменится на новый адрес
            UpdateCementServerIfRequestWasRedirected(requestUri, request.RequestUri);

            using var contentStream = response.Content.ReadAsStream(cts.Token);
            return contentStream.ReadAllBytesWithProgress(ReportProgress);

            void ReportProgress(long totalBytes)
            {
                var readableBytes = totalBytes.Bytes().ToString("0.00");
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

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private void UpdateCementServerIfRequestWasRedirected(Uri requestUri, Uri effectiveRequestUri)
    {
        if (requestUri == effectiveRequestUri)
            return;

        log.LogDebug("Request was redirected, '{RequestUri}' -> '{NewRequestUri}'", requestUri, effectiveRequestUri);

        var newServerUri = effectiveRequestUri.GetLeftPart(UriPartial.Authority);
        log.LogDebug("New server url: {CementServerUri}", newServerUri);

        var settings = CementSettingsRepository.Get();
        settings.CementServer = newServerUri;

        CementSettingsRepository.Save(settings);

        consoleWriter.WriteInfo($"CementServer has been updated: {Name} => {newServerUri}");
        Name = newServerUri;
    }
}
