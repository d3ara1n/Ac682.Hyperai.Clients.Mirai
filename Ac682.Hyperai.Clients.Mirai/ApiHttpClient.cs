using Hyperai.Messages;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class ApiHttpClient
    {
        public string BaseUrl { get; private set; }
        public string UserAgent => $"AHCMirai/{Assembly.GetExecutingAssembly().GetName().Version} Hyperai/{Assembly.GetAssembly(typeof(MessageChain)).GetName().Version} CLR/{Assembly.GetAssembly(typeof(object)).GetName().Version}";

        private readonly HttpClient client;

        public ApiHttpClient(string baseUrl)
        {
            BaseUrl = baseUrl;
            client = new HttpClient() { BaseAddress = new Uri(baseUrl) };
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public async Task<HttpResponseMessage> GetAsync(string action)
        {
            return await client.GetAsync(action);
        }

        public async Task<HttpResponseMessage> PostAsync(string action, HttpContent content)
        {
            return await client.PostAsync(action, content);
        }

        public async Task<HttpResponseMessage> PostObjectAsync(string action, object body)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Converters.Add(new MessageChainJsonConverter());
            string content = JsonConvert.SerializeObject(body, settings);
            return await PostAsync(action, new StringContent(content));
        }
    }
}