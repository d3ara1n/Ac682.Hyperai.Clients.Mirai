using Hyperai.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ac682.Hyperai.Clients.Mirai
{
    public static class ClientExtensions
    {
        public static IServiceCollection AddMiraiClient(this IServiceCollection services, Action<MiraiClientOptions> optionsBuilder)
        {
            services.AddSingleton<IApiClient, MiraiClient>().Configure(optionsBuilder);
            return services;
        }
    }
}
