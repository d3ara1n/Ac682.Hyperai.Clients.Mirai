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

        private readonly List<(Type, object)> handlers = new List<(Type, object)>();

        private readonly MiraiHttpSession _session;

        private readonly MiraiClientOptions _options;
        private readonly ILogger<MiraiClient> _logger;
        private readonly IMessageChainFormatter _formatter;

        public MiraiClient(MiraiClientOptions options, ILogger<MiraiClient> logger, IMessageChainFormatter formatter)
        {
            _options = options;
            _logger = logger;
            _formatter = formatter;
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
            foreach (object handler in handlers.Where(x => x.Item1.IsAssignableFrom(typeof(T))).Select(x => x.Item2))
            {
                handler.GetType().GetMethod("Handle").Invoke(handler, new object[] { args });
            }
        }

        private void InvokeHandler(GenericEventArgs args)
        {
            foreach (object handler in handlers.Where(x => x.Item1.IsAssignableFrom(args.GetType())).Select(x => x.Item2))
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

        private readonly bool isDisposed = false;
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
                GenericEventArgs evt = _session.PullEvent();
                if (evt != null)
                {
                    _logger.LogInformation("Event received: " + evt);
                    InvokeHandler(evt);
                }else
                {
                	Thread.Sleep(10);
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

        public async Task<T> RequestAsync<T>(T id)
        {
            if (typeof(T) == typeof(Group))
            {
                return ChangeType<T>((await _session.GetGroupInfoAsync(ChangeType<Group>(id).Identity))) ?? id;
            }
            else if (typeof(T) == typeof(Self))
            {
                return ChangeType<T>(new Self() { Groups = new Lazy<IEnumerable<Group>>(() => _session.GetGroupsAsync().GetAwaiter().GetResult()), Friends = new Lazy<IEnumerable<Friend>>(() => _session.GetFriendsAsync().GetAwaiter().GetResult())}) ?? id;
            }
            else if (typeof(T) == typeof(Member))
            {
                return ChangeType<T>(await _session.GetMemberInfoAsync(ChangeType<Member>(id).Identity, ChangeType<Member>(id).Group.Value.Identity)) ?? id;
            }
            return id;
        }

        private T ChangeType<T>(object o)
        {
            if (o == null)
            {
                return default(T);
            }

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
                case MemberRequestResponsedEventArgs mRespose:
                    await _session.SendMemberRequestResponsedAsync(mRespose.EventId, mRespose.FromWhom, mRespose.InWhichGroup, mRespose.Operation, mRespose.MessageToAttach);
                    break;
                case FriendRequestResponsedEventArgs fRespose:
                    await _session.SendFriendRequestResponsedAsync(fRespose.EventId, fRespose.FromWhom, fRespose.FromWhichGroup, fRespose.Operation, fRespose.MessageToAttach);
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
