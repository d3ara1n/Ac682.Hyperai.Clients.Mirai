﻿using Hyperai.Events;
using Hyperai.Relations;
using Hyperai.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiClient : IApiClient
    {
        public ApiClientConnectionState State => _session.State;

        List<(Type, object)> handlers = new List<(Type, object)>();

        private readonly MiraiHttpSession _session;

        private readonly MiraiClientOptions _options;
        private readonly ILogger<MiraiClient> _logger;

        public MiraiClient(MiraiClientOptions options, ILogger<MiraiClient> logger)
        {
            _options = options;
            _logger = logger;

            _session = new MiraiHttpSession(options.Host, options.Port, options.AuthKey, options.SelfQQ);
        }

        public void Connect()
        {
            _logger.LogInformation("Connecting to {0}:{1}", _options.Host, _options.Port);
            _session.Connect();
            _logger.LogInformation("Connected.");
        }

        private void InvokeHandler<T>(T args) where T : GenericEventArgs
        {
            foreach (var handler in handlers.Where(x => x.Item1.IsAssignableFrom(typeof(T))).Select(x => x.Item2))
            {
                handler.GetType().GetMethod("Handle").Invoke(handler, new object[] { args });
            }
        }

        private void InvokeHandler(GenericEventArgs args)
        {
            foreach (var handler in handlers.Where(x => x.Item1.IsAssignableFrom(args.GetType())).Select(x => x.Item2))
            {
                handler.GetType().GetMethod("Handle").Invoke(handler, new object[] { args });
            }
        }

        public void Disconnect()
        {
            _session.Disconnect();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool isDisposed = false;
        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed && isDisposing)
            {
                isDisposing = true;
                _session.Dispose();
            }
        }

        public void Listen()
        {
            while (State == ApiClientConnectionState.Connected)
            {
                var evt = _session.PullEvent();
                if (evt != null)
                {
                    InvokeHandler(evt);
                }
            }
            Disconnect();
        }

        public void On<TEventArgs>(IEventHandler<TEventArgs> handler) where TEventArgs : GenericEventArgs
        {
            handlers.Add((typeof(TEventArgs), handler));
        }

        [Obsolete]
        public IQueryable<T> Query<T>(Func<T, bool> cond) where T : class, new()
        {
            throw new NotImplementedException();
        }

        async Task<T> IApiClient.RequestAsync<T>(T id)
        {
            if (typeof(T) == typeof(Friend))
            {
                return (T)Convert.ChangeType((await _session.GetFriendsAsync()).Where(x => x.Identity == ((Friend)Convert.ChangeType(id, typeof(Friend))).Identity).FirstOrDefault(), typeof(T));
            }
            else if (typeof(T) == typeof(Group))
            {
                return (T)Convert.ChangeType((await _session.GetGroupsAsync()).Where(x => x.Identity == ((Group)Convert.ChangeType(id, typeof(Group))).Identity).FirstOrDefault(), typeof(T));
            }
            return default(T);
        }

        public async Task SendAsync<TEventArgs>(TEventArgs args) where TEventArgs : MessageEventArgs
        {
            switch (args)
            {
                case FriendMessageEventArgs friendMessage:
                    await _session.SendFriendMessageAsync(friendMessage.User, friendMessage.Message);
                    break;
                case GroupMessageEventArgs groupMessage:
                    await _session.SendGroupMessageAsync(groupMessage.Group, groupMessage.Message);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public string RequestRawAsync(string resource)
        {
            throw new NotImplementedException();
        }

        public void SendRawAsync(string resource)
        {
            throw new NotImplementedException();
        }
    }
}
