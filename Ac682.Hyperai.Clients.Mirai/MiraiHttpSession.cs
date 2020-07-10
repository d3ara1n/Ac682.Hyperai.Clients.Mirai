using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ac682.Hyperai.Clients.Mirai.DtObjects;
using Ac682.Hyperai.Clients.Mirai.Serialization;
using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Relations;
using Hyperai.Services;
using Newtonsoft.Json;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiHttpSession : IDisposable
    {
        public ApiClientConnectionState State { get; private set; } = ApiClientConnectionState.Disconnected;
        private readonly string _host;
        private readonly int _port;
        private readonly string _authKey;
        private readonly long _selfQQ;


        private readonly HttpClient _client;
        private readonly JsonFormatter _formatter;
        private readonly JsonParser _parser;
        private string sessionKey = null;

        public MiraiHttpSession(string host, int port, string authKey, long selfQQ)
        {
            _host = host;
            _port = port;
            _authKey = authKey;
            _selfQQ = selfQQ;

            _client = new HttpClient($"{host}:{port}");
        }

        public GenericEventArgs PullEvent()
        {
            // $"fetchLatestMessage?sessionKey={sessionKey}&count=1".MakeRequest()
            // .Get(_client)
            // .IfErrorThenThrow()
            // .HandleData();

            var fetch = _client.GetAsync($"fetchLatestMessage?sessionKey={sessionKey}&count=1").GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            foreach (var evt in fetch.data)
            {
                switch (evt.type)
                {
                    case "FriendMessage":
                        return new FriendMessageEventArgs()
                        {
                            Message = _parser.Parse(evt.messageChain.ToString()),
                            User = new Friend()
                            {
                                Identity = evt.sender.id,
                                Nickname = evt.sender.nickname,
                                Remark = evt.sender.remark
                            }
                        };
                    case "GroupMessage":
                        return new GroupMessageEventArgs()
                        {
                            Message = _parser.Parse(evt.messageChain.ToString()),
                            User = new Member()
                            {
                                Identity = evt.sender.id,
                                DisplayName = evt.sender.memberName,
                            },
                            Group = new Group()
                            {
                                Identity = evt.sender.group.id,
                                Name = evt.sender.group.name,
                            }
                        };
                    default:
                        throw new NotImplementedException();
                }
            }
            return null;
        }

        public void Connect()
        {

            var auth = _client.PostAsync("auth", new { authKey = _authKey }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            sessionKey = auth.session;
            if (auth.code == -1)
            {
                throw new ArgumentException("Wrong MIRAI API HTTP auth key");
            }

            var verify = _client.PostAsync("verify", new { sessionKey = this.sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            var code = (ErrorCode)Enum.ToObject(typeof(ErrorCode), verify.code);
            if (code != ErrorCode.Normal)
            {
                throw new ArgumentException(Enum.GetName(typeof(ErrorCode), code));
            }

            State = ApiClientConnectionState.Connected;
        }

        public async Task<IEnumerable<Friend>> GetFriendsAsync()
        {
            var friends = await (await _client.GetAsync($"friendList?sessionKey={sessionKey}")).GetJsonObjectAsync();
            var list = new List<Friend>();
            foreach (var friend in friends)
            {
                list.Add(new Friend()
                {
                    Identity = friend.id,
                    Nickname = friend.nickname,
                    Remark = friend.remark
                });
            }
            return list;
        }

        public async Task<IEnumerable<Group>> GetGroupsAsync()
        {
            var groups = await (await _client.GetAsync($"groupList?sessionKey={sessionKey}")).GetJsonObjectAsync();
            var list = new List<Group>();
            foreach (var group in groups)
            {
                list.Add(new Group()
                {
                    Identity = group.id,
                    Name = group.name,
                });
            }
            return list;
        }

        public async Task<int> SendFriendMessageAsync(Friend friend, MessageChain chain)
        {
            var message = await (await _client.PostAsync("sendFriendMessage", new { sessionKey = this.sessionKey, target = friend.Identity, messageChain = _formatter.Format(chain) })).GetJsonObjectAsync();
            if (message.code != 0)
            {
                throw new Exception(message.msg);
            }
            return message.messageId;
        }

        public async Task<int> SendGroupMessageAsync(Group group, MessageChain chain)
        {
            var message = await (await _client.PostAsync("sendGroupMessage", new { sessionKey = this.sessionKey, target = group.Identity, messageChain = _formatter.Format(chain) })).GetJsonObjectAsync();
            if (message.code != 0)
            {
                throw new Exception(message.msg);
            }
            return message.messageId;
        }

        public void Disconnect()
        {
            if (State == ApiClientConnectionState.Connected)
            {
                var release = _client.PostAsync("release", new { sessionKey = this.sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
                State = ApiClientConnectionState.Disconnected;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && !isDisposed)
            {
                // do something
                if (State == ApiClientConnectionState.Connected)
                {
                    Disconnect();
                }
            }
        }
    }
}