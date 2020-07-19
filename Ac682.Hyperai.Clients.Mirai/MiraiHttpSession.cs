using Ac682.Hyperai.Clients.Mirai.Serialization;
using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Hyperai.Relations;
using Hyperai.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
            JToken fetch = _client.GetAsync($"fetchLatestMessage?sessionKey={sessionKey}&count=1").GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            foreach (JToken evt in fetch.Value<JArray>("data"))
            {
                #region 事件工厂
                switch (evt.Value<string>("type"))
                {
                    case "FriendMessage":
                        {
                            var args = new FriendMessageEventArgs()
                            {
                                Message = _parser.Parse(evt.Value<JArray>("messageChain").ToString()),
                                User = new Friend()
                                {
                                    Identity = evt["sender"].Value<long>("id"),
                                    Nickname = evt["sender"].Value<string>("nickname"),
                                    Remark = evt["sender"].Value<string>("remark")
                                }
                            };
                            return args;
                        }
                    case "GroupMessage":
                        {
                            var args = new GroupMessageEventArgs()
                            {
                                Message = _parser.Parse(evt.Value<JArray>("messageChain").ToString()),
                                User = OfMember(evt["sender"], evt["group"]),
                            };
                            args.Group = args.User.Group;
                            return args;
                        }
                    case "GroupRecallEvent":
                        {
                            var args = new GroupRecallEventArgs()
                            {
                                WhoseMessage = evt.Value<long>("authorId"),
                                MessageId = evt.Value<long>("messageId"),
                                Operator = OfMember(evt["operator"], evt["group"])
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "FriendRecallEvent":
                        {
                            var args = new FriendRecallEventArgs()
                            {
                                MessageId = evt.Value<long>("messageId"),
                                Operator = evt.Value<long>("operator"),
                                WhoseMessage = evt.Value<long>("authorId")
                            };
                            return args;
                        }
                    case "BotGroupPermissionChangeEvent":
                        {
                            var args = new GroupSelfPermissionChangedEventArgs()
                            {
                                Original = OfRole(evt.Value<string>("origin")),
                                Present = OfRole(evt.Value<string>("current")),
                            };
                            args.Group = OfGroup(evt["group"]);
                            return args;
                        }
                    case "BotMuteEvent":
                        {
                            var args = new GroupSelfMutedEventArgs()
                            {
                                Duration = evt.Value<long>("duration"),
                                Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "BotUnmuteEvent":
                        {
                            var args = new GroupSelfUnmutedEventArgs()
                            {
                                Operator = OfMember(evt["operator"], evt["opeartor"]["group"])
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "BotLeaveEventActive":
                        {
                            var args = new GroupSelfLeftEventArgs()
                            {
                                IsKicked = false,
                                Operator = GetMemberInfoAsync(_selfQQ, evt["group"].Value<long>("id")).GetAwaiter().GetResult()
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "BotLeaveEventKick":
                        {
                            var args = new GroupSelfLeftEventArgs()
                            {
                                IsKicked = true,
                                Operator = GetMemberInfoAsync(_selfQQ, evt["group"].Value<long>("id")).GetAwaiter().GetResult()
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "GroupNameChangeEvent":
                        {
                            var args = new GroupNameChangedEventArgs()
                            {
                                Original = evt.Value<string>("origin"),
                                Present = evt.Value<string>("current"),
                                Operator = OfMember(evt["operator"], evt["group"])
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "GroupMuteAllEvent":
                        {
                            var args = new GroupAllMutedEventArgs()
                            {
                                IsEnded = !evt.Value<bool>("current"),
                                Operator = OfMember(evt["operator"], evt["group"]),
                            };
                            args.Group = args.Operator.Group;
                            return args;
                        }
                    case "MemberJoinEvent":
                        {
                            var args = new GroupMemberJoinedEventArgs()
                            {
                                Who = OfMember(evt["member"], evt["member"]["group"])
                            };
                            args.Group = args.Who.Group;
                            return args;
                        }
                    case "MemberLeaveEventKick":
                        {
                            var args = new GroupMemberLeftEventArgs()
                            {
                                IsKicked = true,
                                Who = OfMember(evt["member"], evt["member"]["group"]),
                                Operator = OfMember(evt["operator"], evt["operator"]["group"])
                            };
                            args.Group = args.Who.Group;
                            return args;
                        }
                    case "MemberLeaveEventQuit":
                        {
                            var args = new GroupMemberLeftEventArgs()
                            {
                                IsKicked = false,
                                Who = OfMember(evt["member"], evt["member"]["group"]),
                            };
                            args.Operator = args.Who;
                            args.Group = args.Who.Group;
                            return args;
                        }
                    case "MemberCardChangeEvent":
                        {
                            var args = new GroupMemberCardChangedEventArgs()
                            {
                                IsSelfOperated = false,
                                Original = evt.Value<string>("origin"),
                                Present = evt.Value<string>("current"),
                                Operator = OfMember(evt["operator"], evt["operator"]["group"])
                            };
                            args.Group = args.WhoseName.Group;
                            return args;
                        }
                    case "MemberSpecialTitleChangeEvent":
                        {
                            var args = new GroupMemberTitleChangedEventArgs()
                            {
                                Original = evt.Value<string>("origin"),
                                Present = evt.Value<string>("current"),
                                Who = OfMember(evt["member"], evt["member"]["group"])
                            };
                            args.Group = args.Who.Group;
                            return args;
                        }
                    case "MemberPermissionChangeEvent":
                        {
                            var args = new GroupMemberPermissionChangedEventArgs()
                            {
                                Original = OfRole(evt.Value<string>("origin")),
                                Present = OfRole(evt.Value<string>("current")),
                                Whom = OfMember(evt["member"], evt["member"]["group"])
                            };
                            args.Group = args.Whom.Group;
                            return args;
                        }
                    case "MemberMuteEvent":
                        {
                            var args = new GroupMemberMutedEventArgs()
                            {
                                Duration = evt.Value<long>("duration"),
                                Whom = OfMember(evt["member"], evt["member"]["group"]),
                                Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                            };
                            args.Group = args.Whom.Group;
                            return args;
                        }
                    case "MemberUnmuteEvent":
                        {
                            var args = new GroupMemberUnmutedEventArgs()
                            {
                                Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                                Whom = OfMember(evt["member"], evt["member"]["group"])
                            };
                            args.Group = args.Whom.Group;
                            return args;
                        }
                    case "NewFriendRequestEvent":
                        {
                            var args = new FriendRequestEventArgs()
                            {
                                EventId = evt.Value<long>("eventId"),
                                FromWhom = evt.Value<long>("fromId"),
                                FromWhichGroup = evt.Value<long>("groupId"),
                                DisplayName = evt.Value<string>("nick"),
                                AttachedMessage = evt.Value<string>("message")
                            };
                            return args;
                        }
                    case "MemberJoinRequestEvent":
                        {
                            var args = new GroupMemberRequestEventArgs()
                            {
                                EventId = evt.Value<long>("eventId"),
                                FromWhom = evt.Value<long>("fromId"),
                                InWhichGroup = evt.Value<long>("groupId"),
                                DisplayName = evt.Value<string>("nick"),
                                GroupDisplayName = evt.Value<string>("groupName"),
                                AttachedMessage = evt.Value<string>("message")
                            };
                            return args;
                        }
                    case "BotInvitedJoinGroupRequestEvent":
                        {
                            var args = new SelfInvitedIntoGroupEventArgs()
                            {
                                EventId = evt.Value<long>("eventId"),
                                IntoWhichGroup = evt.Value<long>("groupId"),
                                OperatorId = evt.Value<long>("fromId"),
                                OperatorDisplayName = evt.Value<string>("nick"),
                                AttachedMessage = evt.Value<string>("message"),
                            };
                            return args;
                        }
                    default:
                        break;
                }
                #endregion
            }
            GroupRole OfRole(string name)
            {
                return name switch
                {
                    "OWNER" => GroupRole.Owner,
                    "ADMINISTRATOR" => GroupRole.Administrator,
                    _ => GroupRole.Member
                };
            }
            Member OfMember(JToken member, JToken group)
            {
                var res = new Member()
                {
                    Identity = member.Value<long>("id"),
                    DisplayName = member.Value<string>("memberName"),
                    Role = OfRole(member.Value<string>("permission"))
                };
                res.Group = OfGroup(group);
                return res;
            }
            Group OfGroup(JToken group)
            {
                var res = new Group()
                {
                    Identity = group.Value<long>("id"),
                    Name = group.Value<string>("name"),
                };
                res.Members = GetMembersAsync(res).GetAwaiter().GetResult();
                res.Owner = res.Members.FirstOrDefault(x => x.Role == GroupRole.Owner);
                return res;
            }
            return null;
        }

        public void Connect()
        {

            JToken auth = _client.PostObjectAsync("auth", new { authKey = _authKey }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            sessionKey = auth.Value<string>("session");
            if (auth.Value<int>("code") == -1)
            {
                throw new ArgumentException("Wrong MIRAI API HTTP auth key");
            }

            JToken verify = _client.PostObjectAsync("verify", new { sessionKey = sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            if (verify.Value<int>("code") != 0)
            {
                throw new ArgumentException(verify.Value<string>("msg"));
            }

            State = ApiClientConnectionState.Connected;
        }

        public async Task<IEnumerable<Friend>> GetFriendsAsync()
        {
            try
            {
                JToken friends = await (await _client.GetAsync($"friendList?sessionKey={sessionKey}")).GetJsonObjectAsync();
                List<Friend> list = new List<Friend>();
                foreach (JToken friend in friends)
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
            catch
            {
                return Enumerable.Empty<Friend>();
            }
        }

        public async Task<Member> GetMemberInfoAsync(long memberId, long groupId)
        {
            try
            {
                JToken info = await (await _client.GetAsync($"memberInfo?sessionKey={sessionKey}&target={groupId}&memberId={memberId}")).GetJsonObjectAsync();
                var member = new Member()
                {
                    Identity = memberId,
                    Nickname = info.Value<string>("name"),
                    Title = info.Value<string>("specialTitle"),
                    Role = GroupRole.Member, // 无法从 api 中得知
                    Group = await GetGroupInfoAsync(groupId)
                };
                return member;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Group> GetGroupInfoAsync(long groupId)
        {
            try
            {
                JToken info = await (await _client.GetAsync($"groupConfig?sessionKey={sessionKey}&target={groupId}")).GetJsonObjectAsync();
                var group = new Group()
                {
                    Name = info.Value<string>("name"),
                };
                group.Members = await GetMembersAsync(group);
                group.Owner = group.Members.FirstOrDefault(x => x.Role == GroupRole.Owner);
                return group;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Group>> GetGroupsAsync()
        {
            try
            {
                JToken groups = await (await _client.GetAsync($"groupList?sessionKey={sessionKey}")).GetJsonObjectAsync();
                List<Group> list = new List<Group>();
                foreach (JToken group in groups)
                {
                    Group ins = new Group()
                    {
                        Identity = group.Value<long>("id"),
                        Name = group.Value<string>("name")
                    };
                    var members = await GetMembersAsync(ins);
                    ins.Members = members;
                    ins.Members = Enumerable.Empty<Member>();
                    list.Add(ins);
                }
                return list;
            }
            catch
            {
                return Enumerable.Empty<Group>();
            }
        }

        private async Task<IEnumerable<Member>> GetMembersAsync(Group group)
        {
            try
            {
                JToken members = await (await _client.GetAsync($"memberList?sessionKey={sessionKey}")).GetJsonObjectAsync();
                List<Member> list = new List<Member>();
                foreach (JToken member in members)
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
            catch
            {
                return Enumerable.Empty<Member>();
            }
        }

        public async Task<long> SendFriendMessageAsync(Friend friend, MessageChain chain)
        {
            await PreprocessChainAsync(chain, MessageEventType.Friend);
            HttpResponseMessage response = await _client.PostObjectAsync("sendFriendMessage", new { sessionKey = sessionKey, target = friend.Identity, messageChain = chain.AsReadable() });
            JToken message = await response.GetJsonObjectAsync();
            if (message.Value<int>("code") != 0)
            {
                throw new Exception(message.Value<string>("msg"));
            }
            return message.Value<long>("messageId");
        }

        public async Task<long> SendGroupMessageAsync(Group group, MessageChain chain)
        {
            await PreprocessChainAsync(chain, MessageEventType.Group);
            HttpResponseMessage response = await _client.PostObjectAsync("sendGroupMessage", new { sessionKey = sessionKey, target = group.Identity, messageChain = chain.AsReadable() });
            JToken message = await response.GetJsonObjectAsync();
            if (message.Value<int>("code") != 0)
            {
                throw new Exception(message.Value<string>("msg"));
            }
            return message.Value<long>("messageId");
        }

        public async Task SendMemberRequestResponsedAsync(long eventId, long userId, long groupId, MemberRequestResponseOperationType action, string message)
        {
            await _client.PostObjectAsync("resp/memberJoinRequestEvent", new {sessionKey = this.sessionKey, eventId = eventId, fromId = userId, groupId = groupId, operate = (int)action, message = message});
        }

        public async Task SendFriendRequestResponsedAsync(long eventId, long userId, long groupId, FriendRequestResponseOperationType action, string message)
        {
            await _client.PostObjectAsync("resp/newFriendRequestEvent", new {sessionKey = this.sessionKey, eventId   = eventId, fromId   = userId, groupId = groupId, operate = (int)action, message= message});
        }

        private async Task PreprocessChainAsync(MessageChain chain, MessageEventType type)
        {
            foreach (MessageComponent cmp in chain)
            {
                if (cmp is Image image && string.IsNullOrEmpty(image.ImageId))
                {
                    await UploadImageAsync(image, type);
                }
            }
        }

        private async Task UploadImageAsync(Image image, MessageEventType type)
        {
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new StringContent(sessionKey), "sessionKey");
            content.Add(new StringContent(type switch { MessageEventType.Friend => "friend", MessageEventType.Group => "group", _ => throw new NotImplementedException() }), "type");
            Stream imageStream = image.OpenRead();
            string format;
            using (System.Drawing.Image img = System.Drawing.Image.FromStream(imageStream))
            {
                format = img.RawFormat.ToString();
                switch (format)
                {
                    case nameof(ImageFormat.Jpeg):
                    case nameof(ImageFormat.Png):
                    case nameof(ImageFormat.Gif):
                        {
                            format = format.ToLower();
                            break;
                        }
                    default:
                        {
                            MemoryStream ms = new MemoryStream();
                            img.Save(ms, ImageFormat.Png);
                            imageStream.Dispose();
                            imageStream = ms;
                            format = "png";
                            break;
                        }
                }
            }
            imageStream.Seek(0, SeekOrigin.Begin);
            HttpContent imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "img",
                FileName = $"{Guid.NewGuid():n}.{format}"
            };
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + format);
            content.Add(imageContent, "img");
            JToken resp = await (await _client.PostAsync("uploadImage", content)).GetJsonObjectAsync();
            image.ImageId = resp.Value<string>("imageId");
            image.Url = new Uri(resp.Value<string>("url"));
        }

        public void Disconnect()
        {
            if (State == ApiClientConnectionState.Connected)
            {
                JToken release = _client.PostObjectAsync("release", new { sessionKey = sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
                State = ApiClientConnectionState.Disconnected;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly bool isDisposed = false;
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