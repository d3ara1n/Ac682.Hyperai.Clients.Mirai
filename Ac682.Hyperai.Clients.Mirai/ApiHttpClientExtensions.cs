using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public static class ApiHttpClientExtensions
    {
        public async static Task<string> GetStringAsync(this HttpResponseMessage response)
        {
            return await response.Content.ReadAsStringAsync();
        }

        public async static Task<JToken> GetJsonObjectAsync(this HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JToken>(json);
        }
    }
}