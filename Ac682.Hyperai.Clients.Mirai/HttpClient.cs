using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Hyperai.Messages;
using Newtonsoft.Json;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class HttpClient
    {
        public string BaseUrl { get; private set; }
        public string UserAgent => $"AHCMirai/{Assembly.GetExecutingAssembly().GetName().Version} Hyperai/{Assembly.GetAssembly(typeof(MessageChain)).GetName().Version} CLR/{Assembly.GetAssembly(typeof(object)).GetName().Version}";

        public HttpClient(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public async Task<WebResponse> GetAsync(string action)
        {
            var req = WebRequest.CreateHttp($"{BaseUrl}/{action}");
            req.Method = "GET";
            req.UserAgent = UserAgent;
            return await req.GetResponseAsync();
        }

        public async Task<WebResponse> PostAsync(string action, object body)
        {
            var req = WebRequest.CreateHttp($"{BaseUrl}/{action}");
            req.Method = "POST";
            req.UserAgent = UserAgent;
            var content = JsonConvert.SerializeObject(body);
            req.ContentType = "application/json";
            using(var stream = new StreamWriter(req.GetRequestStream()))
            {
                stream.Write(content);
            }
            return await req.GetResponseAsync();
        }
    }
}