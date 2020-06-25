using Hyperai.Events;
using Hyperai.Relations;
using Hyperai.Services;
using Mirai_CSharp;
using Model = Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiClient : IApiClient
    {
        public ApiClientConnectionState State { get; private set; }

        List<(Type, object)> handlers = new List<(Type, object)>();

        private MiraiHttpSession session;

        private readonly MiraiClientOptions _options;

        public MiraiClient(MiraiClientOptions options)
        {
            _options = options;
        }

        public void Connect()
        {
            var sessionOptions = new Model.MiraiHttpSessionOptions(_options.Host, _options.Port, _options.AuthKey);
            session = new MiraiHttpSession();
            session.ConnectAsync(sessionOptions, _options.SelfQQ).Wait();
            session.FriendMessageEvt += Session_FriendMessageEvtAsync;
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
                handler.GetType().GetMethod("Handler").Invoke(handler, new object[] { args });
            }
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
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

                session?.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        public void Listen()
        {
            throw new NotImplementedException();
        }

        public void On<TEventArgs>(IEventHandler<TEventArgs> handler) where TEventArgs : GenericEventArgs
        {
            handlers.Add((typeof(TEventArgs), handler));
        }

        public IQueryable<T> Query<T>(Func<T, bool> cond) where T : class, new()
        {
            throw new NotImplementedException();
        }

        public T Request<T>() where T : RelationModel
        {
            throw new NotImplementedException();
        }

        public string RequestRaw(string resource)
        {
            throw new NotImplementedException();
        }

        public void Send<TEventArgs>(TEventArgs args) where TEventArgs : MessageEventArgs
        {
            throw new NotImplementedException();
        }

        public void SendRaw(string resource)
        {
            throw new NotImplementedException();
        }
    }
}
