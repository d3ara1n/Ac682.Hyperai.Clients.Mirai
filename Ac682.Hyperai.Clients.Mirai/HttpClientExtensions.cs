using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ac682.Hyperai.Clients.Mirai
{
    public static class HttpClientExtensions
    {
        public async static Task<string> GetStringAsync(this WebResponse response)
        {
            try
            {
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }catch(Exception e)
            {
                throw e;
            }
        }

        public async static Task<dynamic> GetJsonObjectAsync(this WebResponse response)
        {
                        try
            {
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var json = await reader.ReadToEndAsync();
                return JObject.Parse(json);
            }catch(Exception e)
            {
                throw e;
            }
        }
    }
}