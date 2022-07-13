using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common
{
    public sealed class UsagesProvider : IUsagesProvider
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ILogger logger;
        private readonly CementSettings cementSettings;

        public UsagesProvider(ILogger<UsagesProvider> logger, Func<CementSettings> optionsAccessor)
        {
            this.logger = logger;
            cementSettings = optionsAccessor();
        }

        public ShowParentsAnswer GetUsages(string moduleName, string checkingBranch, string configuration = "*")
        {
            logger.LogInformation(
                "Try to get usages, module='{ModuleName}', branch='{ModuleBranch}', " +
                "configuration='{ModuleConfiguration}'", moduleName, checkingBranch, configuration);

            try
            {
                using var cts = new CancellationTokenSource(DefaultTimeout);

                using var handler = new SocketsHttpHandler();
                using var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(cementSettings.CementServer)
                };

                var uri = new Uri($"api/v1/{moduleName}/deps/{configuration}/{checkingBranch}", UriKind.Relative);
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
                var answer = JsonConvert.DeserializeObject<ShowParentsAnswer>(content);

                return answer;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex, "Unable to get usages, module='{ModuleName}', branch='{ModuleBranch}', " +
                        "configuration='{ModuleConfiguration}'", moduleName, checkingBranch, configuration);

                return null;
            }
        }
    }
}
