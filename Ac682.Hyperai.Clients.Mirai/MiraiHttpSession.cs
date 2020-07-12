using Ac682.Hyperai.Clients.Mirai.Serialization;
using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Relations;
using Hyperai.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MiraiHttpSession : IDisposable
    {
        public ApiClientConnectionState State { get; private set; } = ApiClientConnectionState.Disconnected;
        private readonly string _host;
        private readonly int _port;
        private readonly string _authKey;
        private readonly long _selfQQ;


        private readonly ApiHttpClient _client;
        private readonly JsonFormatter _formatter = new JsonFormatter();
        private readonly JsonParser _parser = new JsonParser();
        private string sessionKey = null;

        public MiraiHttpSession(string host, int port, string authKey, long selfQQ)
        {
            _host = host;
            _port = port;
            _authKey = authKey;
            _selfQQ = selfQQ;

            _client = new ApiHttpClient($"http://{host}:{port}");
        }

        public GenericEventArgs PullEvent()
        {
            var fetch = _client.GetAsync($"fetchLatestMessage?sessionKey={sessionKey}&count=1").GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            foreach (var evt in fetch.Value<JArray>("data"))
            {
                switch (evt.Value<string>("type"))
                {
                    case "FriendMessage":
                        return new FriendMessageEventArgs()
                        {
                            Message = _parser.Parse(evt.Value<JArray>("messageChain").ToString()),
                            User = new Friend()
                            {
                                Identity = evt["sender"].Value<long>("id"),
                                Nickname = evt["sender"].Value<string>("nickname"),
                                Remark = evt["sender"].Value<string>("remark")
                            }
                        };
                    case "GroupMessage":
                        var args = new GroupMessageEventArgs()
                        {
                            Message = _parser.Parse(evt.Value<JArray>("messageChain").ToString()),
                            Group = new Group()
                            {
                                Identity = evt["sender"]["group"].Value<long>("id"),
                                Name = evt["sender"]["group"].Value<string>("name"),
                            },
                            User = new Member()
                            {
                                Identity = evt["sender"].Value<long>("id"),
                                DisplayName = evt["sender"].Value<string>("memberName"),
                                Role = evt["sender"].Value<string>("permission") switch { "OWNER" => GroupRole.Owner, "ADMINISTRATOR" => GroupRole.Administrator, _ => GroupRole.Member }
                            }
                        };
                        args.User.Group = args.Group;
                        return args;
                    default:
                        break;
                }
            }
            return null;
        }

        public void Connect()
        {

            var auth = _client.PostAsync("auth", new { authKey = _authKey }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            sessionKey = auth.Value<string>("session");
            if (auth.Value<int>("code") == -1)
            {
                throw new ArgumentException("Wrong MIRAI API HTTP auth key");
            }

            var verify = _client.PostAsync("verify", new { sessionKey = this.sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            if (verify.Value<int>("code") != 0) throw new ArgumentException(verify.Value<string>("msg"));

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
                    Identity = friend.Value<long>("id"),
                    Nickname = friend.Value<string>("nickname"),
                    Remark = friend.Value<string>("remark")
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
                var ins = new Group()
                {
                    Identity = group.Value<long>("id"),
                    Name = group.Value<string>("name")
                };
                // TODO: uncomment when it wont cause error
                // var members = await GetMembersAsync(ins);
                // ins.Members = members;
                list.Add(ins);
            }
            return list;
        }

        private async Task<IEnumerable<Member>> GetMembersAsync(Group group)
        {
            var members = await (await _client.GetAsync($"memberList?sessionKey={sessionKey}")).GetJsonObjectAsync();
            var list = new List<Member>();
            foreach (var member in members)
            {
                list.Add(new Member()
                {
                    Identity = member.Value<long>("id"),
                    DisplayName = member.Value<string>("memberName"),
                    Role = member.Value<string>("permission") switch { "OWNER" => GroupRole.Owner, "ADMINISTRATOR" => GroupRole.Administrator, _ => GroupRole.Member },
                    Group = group
                });
            }
            return list;
        }

        public async Task<long> SendFriendMessageAsync(Friend friend, MessageChain chain)
        {
            var response = await _client.PostAsync("sendFriendMessage", new { sessionKey = this.sessionKey, target = friend.Identity, messageChain = chain.AsReadable() });
            var message = await response.GetJsonObjectAsync();
            if (message.Value<int>("code") != 0)
            {
                throw new Exception(message.Value<string>("msg"));
            }
            return message.Value<long>("messageId");
        }

        public async Task<long> SendGroupMessageAsync(Group group, MessageChain chain)
        {
            var response = await _client.PostAsync("sendGroupMessage", new { sessionKey = this.sessionKey, target = group.Identity, messageChain = chain.AsReadable() });
            var message = await response.GetJsonObjectAsync();
            if (message.Value<int>("code") != 0)
            {
                throw new Exception(message.Value<string>("msg"));
            }
            return message.Value<long>("messageId");
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