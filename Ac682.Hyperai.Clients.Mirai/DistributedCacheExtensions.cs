using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public static class DistributedCacheExtensions
    {
        public static T GetObject<T>(this IDistributedCache cache, string key)
        {
            return cache.GetObjectAsync<T>(key).GetAwaiter().GetResult();
        }

        public static void SetObject<T>(this IDistributedCache cache, string key, T obj)
        {
            cache.SetObjectAsync<T>(key, obj).Wait();
        }

        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            await cache.SetStringAsync(key, json);
        }

        public static async Task<T> GetObjectAsync<T>(this IDistributedCache cache, string key)
        {
            string json = await cache.GetStringAsync(key);
            if (json != null)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }
    }
}
