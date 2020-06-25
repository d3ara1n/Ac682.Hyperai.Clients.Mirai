using Hyperai.Events;
using Hyperai.Relations;
using Hyperai.Services;
using Mirai_CSharp;
using Model = Mirai_CSharp.Models;
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
        public ApiClientConnectionState State { get; private set; }

        List<(Type, object)> handlers = new List<(Type, object)>();

        private MiraiHttpSession session;

        private readonly MiraiClientOptions _options;
        private readonly ILogger<MiraiClient> _logger;

        public MiraiClient(MiraiClientOptions options, ILogger<MiraiClient> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Connect()
        {
            State = ApiClientConnectionState.Connecting;
            _logger.LogInformation("Connecting to {0}:{1}", _options.Host, _options.Port);
            var sessionOptions = new Model.MiraiHttpSessionOptions(_options.Host, _options.Port, _options.AuthKey);
            session = new MiraiHttpSession();
            session.ConnectAsync(sessionOptions, _options.SelfQQ);
            session.FriendMessageEvt += Session_FriendMessageEvtAsync;
            State = ApiClientConnectionState.Connected;
            _logger.LogInformation("Connected.");
        }

        private async Task<bool> Session_FriendMessageEvtAsync(MiraiHttpSession sender, Model.IFriendMessageEventArgs e)
        {
            var args = new FriendMessageEventArgs()
            {
                User = e.Sender.ToFriend(),
                Time = DateTime.Now,
                Message = e.Chain.ToMessageChain()
            };
            InvokeHandler(args);
            return true;
        }

        private void InvokeHandler<T>(T args) where T : GenericEventArgs
        {
            foreach (var handler in handlers.Where(x => x.Item1.IsAssignableFrom(typeof(T))).Select(x => x.Item2))
            {
                handler.GetType().GetMethod("Handle").Invoke(handler, new object[] { args });
            }
        }

        public void Disconnect()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool isDisposed = false;
        protected virtual void Dispose(bool isDisposing)
        {
            if(!isDisposed && isDisposing)
            {
                isDisposing = true;
                State = ApiClientConnectionState.Disconnected;
                session?.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        bool goDie = false;

        public void Listen()
        {
            while (!goDie) Thread.Sleep(100);
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

        public async Task<T> RequestAsync<T>(T id) where T : RelationModel
        {
            if(typeof(T) == typeof(Friend))
            {
                return (T)Convert.ChangeType((await session.GetFriendListAsync()).Where(x=>x.Id == id.Identity).FirstOrDefault()?.ToFriend(), typeof(T));
            }

            return null;
        }

        [Obsolete]
        public string RequestRaw(string resource)
        {
            throw new NotImplementedException();
        }

        public async Task SendAsync<TEventArgs>(TEventArgs args) where TEventArgs : MessageEventArgs
        {
            switch(args)
            {
                case FriendMessageEventArgs friendMessage:
                    await session.SendFriendMessageAsync(friendMessage.User.Identity, friendMessage.Message.ToMessageBases().ToArray());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [Obsolete]
        public void SendRaw(string resource)
        {
            throw new NotImplementedException();
        }
    }
}
