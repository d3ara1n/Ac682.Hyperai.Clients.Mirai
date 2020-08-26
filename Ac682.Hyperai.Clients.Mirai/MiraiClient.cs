using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Hyperai.Receipts;
using Hyperai.Relations;
using Hyperai.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiClient : IApiClient
    {
        public ApiClientConnectionState State => _session.State;

        private readonly List<(Type, object)> handlers = new List<(Type, object)>();
        private int waitTime = 3000;

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

        private bool isDisposed = false;

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed && isDisposing)
            {
                isDisposed = true;
                _session.Dispose();
            }
        }

        public void Listen()
        {
            while (State == ApiClientConnectionState.Connected)
            {
                GenericEventArgs evt = null;
                try
                {
                    evt = _session.PullEvent();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception ({}) occurred while pulling event. Next try in {}ms", e.Message, waitTime);
                    Thread.Sleep(waitTime);
                    waitTime *= 2;
                }
                if (evt != null)
                {
                    _logger.LogInformation("Event received: " + evt);
                    InvokeHandler(evt);
                    waitTime = 3000;
                }
                else
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
                return ChangeType<T>(await _session.GetGroupInfoAsync(ChangeType<Group>(id).Identity)) ?? id;
            }
            else if (typeof(T) == typeof(Self))
            {
                return ChangeType<T>(new Self() { Groups = new Lazy<IEnumerable<Group>>(() => _session.GetGroupsAsync().GetAwaiter().GetResult()), Friends = new Lazy<IEnumerable<Friend>>(() => _session.GetFriendsAsync().GetAwaiter().GetResult()) }) ?? id;
            }
            else if (typeof(T) == typeof(Member))
            {
                return ChangeType<T>(await _session.GetMemberInfoAsync(ChangeType<Member>(id).Identity, ChangeType<Member>(id).Group.Value.Identity)) ?? id;
            }
            else if (typeof(T) == typeof(MessageChain))
            {
                return ChangeType<T>(await _session.GetMessageById(((Source)ChangeType<MessageChain>(id).First( x => x is Source)).MessageId)) ?? id;
            }
            return id;
        }

        private T ChangeType<T>(object o)
        {
            if (o == null)
            {
                return default;
            }

            return (T)Convert.ChangeType(o, typeof(T));
        }

        public async Task<GenericReceipt> SendAsync<TArgs>(TArgs args) where TArgs : GenericEventArgs
        {
            GenericReceipt receipt;
            switch (args)
            {
                case FriendMessageEventArgs friendMessage:
                    {
                        long messageId = await _session.SendFriendMessageAsync(friendMessage.User, friendMessage.Message);
                        receipt = new MessageReceipt()
                        {
                            MessageId = messageId
                        };
                        break;
                    }
                case GroupMessageEventArgs groupMessage:
                    {
                        long messageId = await _session.SendGroupMessageAsync(groupMessage.Group, groupMessage.Message);
                        receipt = new MessageReceipt()
                        {
                            MessageId = messageId
                        };
                        break;
                    }
                case MemberRequestResponsedEventArgs mRespose:
                    {
                        await _session.SendMemberRequestResponsedAsync(mRespose.EventId, mRespose.FromWhom, mRespose.InWhichGroup, mRespose.Operation, mRespose.MessageToAttach);
                        receipt = new GenericReceipt();
                        break;
                    }

                case FriendRequestResponsedEventArgs fRespose:
                    {
                        await _session.SendFriendRequestResponsedAsync(fRespose.EventId, fRespose.FromWhom, fRespose.FromWhichGroup, fRespose.Operation, fRespose.MessageToAttach);
                        receipt = new GenericReceipt();
                        break;
                    }

                case RecallEventArgs recall:
                    {
                        await _session.RevokeMessageAsync(recall.MessageId);
                        receipt = new GenericReceipt();
                        break;
                    }

                case GroupMemberLeftEventArgs gLeft:
                    {
                        await _session.KickMemberAsync(gLeft.Who);
                        receipt = new GenericReceipt();
                        break;
                    }


                case GroupSelfLeftEventArgs qiezi:
                    {
                        await _session.QuitGroupAsync(qiezi.Group);
                        receipt = new GenericReceipt();
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }

            _logger.LogInformation("EventArgs sent: {0}", args);
            return receipt;
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