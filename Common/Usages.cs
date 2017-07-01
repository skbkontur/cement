using System.Net;
using Newtonsoft.Json;

namespace Common
{
    public static class Usages
    {
        public static ShowParentsAnswer GetUsagesResponse(string moduleName, string checkingBranch, string configuration="*")
        {
            var webClient = new WebClient();
            var str = webClient.DownloadString($"{CementSettings.Get().CementServer}/api/v1/{moduleName}/deps/{configuration}/{checkingBranch}");
            return JsonConvert.DeserializeObject<ShowParentsAnswer>(str);
        }
    }
}