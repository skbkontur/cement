using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Common
{
    public static class Usages
    {
        public static async Task<ShowParentsAnswer> GetUsagesResponseAsync(string moduleName, string checkingBranch, string configuration="*")
        {
            string url = $"{CementSettings.Get().CementServer}/api/v1/{moduleName}/deps/{configuration}/{checkingBranch}";
            HttpClient client = new HttpClient();

            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    var jsonContent = await content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ShowParentsAnswer>(jsonContent);
                }
            }
        }
    }
}