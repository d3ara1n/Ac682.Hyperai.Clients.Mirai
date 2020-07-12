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

        public async static Task SetObjectAsync<T>(this IDistributedCache cache, string key, T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            await cache.SetStringAsync(key, json);
        }

        public async static Task<T> GetObjectAsync<T>(this IDistributedCache cache, string key)
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
