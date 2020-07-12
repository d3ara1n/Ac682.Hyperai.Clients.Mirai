using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Relations;
using Hyperai.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiClient : IApiClient
    {
        public ApiClientConnectionState State => _session.State;

        List<(Type, object)> handlers = new List<(Type, object)>();

        private readonly MiraiHttpSession _session;

        private readonly MiraiClientOptions _options;
        private readonly ILogger<MiraiClient> _logger;
        private readonly IMessageChainFormatter _formatter;
        private readonly IDistributedCache _cache;

        public MiraiClient(MiraiClientOptions options, ILogger<MiraiClient> logger, IMessageChainFormatter formatter, IDistributedCache cache)
        {
            _options = options;
            _logger = logger;
            _formatter = formatter;
            _session = new MiraiHttpSession(options.Host, options.Port, options.AuthKey, options.SelfQQ);
            _cache = cache;
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
                    _logger.LogInformation("Event received: " + evt);
                    InvokeHandler(evt);
                }
                Thread.Sleep(100);
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

        public async Task<T> RequestAsync<T>(T id)
        {
            if (typeof(T) == typeof(Friend))
            {
                var friend = await _cache.GetObjectAsync<Friend>($"_FRIEND@{ChangeType<Friend>(id).Identifier}");
                if (friend != null)
                {
                    return ChangeType<T>(friend);
                }
                friend = (await _session.GetFriendsAsync()).Where(x => x.Identity == ((Friend)Convert.ChangeType(id, typeof(Friend))).Identity).FirstOrDefault();
                await _cache.SetObjectAsync($"_FRIEND@{ChangeType<Friend>(id).Identifier}", friend);
                return ChangeType<T>(friend);

            }
            else if (typeof(T) == typeof(Group))
            {
                // 💢 Do NOT use JSON to serialize

                //var group = await _cache.GetObjectAsync<Group>($"_GROUP@{ChangeType<Group>(id).Identifier}");
                //if (group != null)
                //{
                //    return ChangeType<T>(group);
                //}

                return ChangeType<T>((await _session.GetGroupsAsync()).Where(x => x.Identity == (ChangeType<Group>(id)).Identity).FirstOrDefault());
            }
            else if (typeof(T) == typeof(Self))
            {
                //var self = await _cache.GetObjectAsync<Self>($"_SELF");
                //if (self != null)
                //{
                //    return ChangeType<T>(self);
                //}
                return ChangeType<T>(new Self() { Groups = await _session.GetGroupsAsync(), Friends = await _session.GetFriendsAsync() });
            }
            else if (typeof(T) == typeof(Member))
            {
                // So does member

                //var member = await _cache.GetObjectAsync<Member>($"_MEMBER@{ChangeType<Member>(id).Identifier}");
                //if (member != null)
                //{
                //    return ChangeType<T>(member);
                //}
                var idMember = ChangeType<Member>(id);
                var group = await RequestAsync(idMember.Group);
                return ChangeType<T>(group.Members.Where(x => x.Identifier == idMember.Identifier).FirstOrDefault());
            }
            return default(T);
        }

        private T ChangeType<T>(object o)
        {
            if (o == null) return default(T);
            return (T)Convert.ChangeType(o, typeof(T));
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
            _logger.LogInformation("EventArgs sendt: {0}", args);
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
