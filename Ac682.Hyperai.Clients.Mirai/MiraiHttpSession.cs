using Ac682.Hyperai.Clients.Mirai.Serialization;
using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Hyperai.Relations;
using Hyperai.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        private readonly string _authKey;
        private readonly long _selfQQ;

        private readonly ApiHttpClient _client;
        private readonly JsonParser _parser = new JsonParser();
        private string sessionKey = null;

        public MiraiHttpSession(string host, int port, string authKey, long selfQQ)
        {
            _authKey = authKey;
            _selfQQ = selfQQ;

            _client = new ApiHttpClient($"http://{host}:{port}");
        }

        public GenericEventArgs PullEvent()
        {
            JToken fetch = _client.GetAsync($"fetchLatestMessage?sessionKey={sessionKey}&count=1").GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            foreach (JToken evt in fetch.Value<JArray>("data"))
            {
                return ReadEventJObject(evt);
            }
            return null;
        }

        private GroupRole OfRole(string name)
        {
            return name switch
            {
                "OWNER" => GroupRole.Owner,
                "ADMINISTRATOR" => GroupRole.Administrator,
                _ => GroupRole.Member
            };
        }

        private Member OfMember(JToken member, JToken group)
        {
            if (member == null) return null;
            var res = new Member()
            {
                Identity = member.Value<long>("id"),
                DisplayName = member.Value<string>("memberName"),
                Role = OfRole(member.Value<string>("permission"))
            };
            res.Group = new Lazy<Group>(() => OfGroup(group));
            return res;
        }

        private Group OfGroup(JToken group)
        {
            var res = new Group()
            {
                Identity = group.Value<long>("id"),
                Name = group.Value<string>("name"),
            };
            res.Members = new Lazy<IEnumerable<Member>>(() => GetMembersAsync(res).GetAwaiter().GetResult());
            res.Owner = new Lazy<Member>(() => res.Members.Value.FirstOrDefault(x => x.Role == GroupRole.Owner));
            return res;
        }

        public GenericEventArgs ReadEventJObject(JToken evt)
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
                            User = OfMember(evt["sender"], evt["sender"]["group"]),
                        };
                        args.Group = args.User.Group.Value;
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
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "FriendRecallEvent":
                    {
                        var args = new FriendRecallEventArgs()
                        {
                            MessageId = evt.Value<long>("messageId"),
                            Operator = evt.Value<long>("operator"),
                            // WhoseMessage = evt.Value<long>("authorId")
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
                            Duration = TimeSpan.FromSeconds(evt.Value<long>("duration")),
                            Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                        };
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "BotUnmuteEvent":
                    {
                        var args = new GroupSelfUnmutedEventArgs()
                        {
                            Operator = OfMember(evt["operator"], evt["opeartor"]["group"])
                        };
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "BotLeaveEventActive":
                    {
                        var args = new GroupSelfLeftEventArgs()
                        {
                            IsKicked = false,
                            Operator = GetMemberInfoAsync(_selfQQ, evt["group"].Value<long>("id")).GetAwaiter().GetResult()
                        };
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "BotLeaveEventKick":
                    {
                        var args = new GroupSelfLeftEventArgs()
                        {
                            IsKicked = true,
                            Operator = GetMemberInfoAsync(_selfQQ, evt["group"].Value<long>("id")).GetAwaiter().GetResult()
                        };
                        args.Group = args.Operator.Group.Value;
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
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "GroupMuteAllEvent":
                    {
                        var args = new GroupAllMutedEventArgs()
                        {
                            IsEnded = !evt.Value<bool>("current"),
                            Operator = OfMember(evt["operator"], evt["group"]),
                        };
                        args.Group = args.Operator.Group.Value;
                        return args;
                    }
                case "MemberJoinEvent":
                    {
                        var args = new GroupMemberJoinedEventArgs()
                        {
                            Who = OfMember(evt["member"], evt["member"]["group"])
                        };
                        args.Group = args.Who.Group.Value;
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
                        args.Group = args.Who.Group.Value;
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
                        args.Group = args.Who.Group.Value;
                        return args;
                    }
                case "MemberCardChangeEvent":
                    {
                        var args = new GroupMemberCardChangedEventArgs()
                        {
                            IsSelfOperated = false,
                            Original = evt.Value<string>("origin"),
                            Present = evt.Value<string>("current"),
                            WhoseName = OfMember(evt["member"], evt["member"]["group"])
                        };
                        // evt["operator"] 永远是 null， 至少我这边如此 args.Operator = evt["operator"] == null
                        // ? null : OfMember(evt["operator"], evt["operator"]["group"]);
                        args.Group = args.WhoseName.Group.Value;
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
                        args.Group = args.Who.Group.Value;
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
                        args.Group = args.Whom.Group.Value;
                        return args;
                    }
                case "MemberMuteEvent":
                    {
                        var args = new GroupMemberMutedEventArgs()
                        {
                            Duration = TimeSpan.FromSeconds(evt.Value<long>("duration")),
                            Whom = OfMember(evt["member"], evt["member"]["group"]),
                            Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                        };
                        args.Group = args.Whom.Group.Value;
                        return args;
                    }
                case "MemberUnmuteEvent":
                    {
                        var args = new GroupMemberUnmutedEventArgs()
                        {
                            Operator = OfMember(evt["operator"], evt["operator"]["group"]),
                            Whom = OfMember(evt["member"], evt["member"]["group"])
                        };
                        args.Group = args.Whom.Group.Value;
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
                    // throw new NotImplementedException(evt.Value<string>("type"));
                    return null;
            }

            #endregion 事件工厂
        }

        public void Connect()
        {
            JToken auth = _client.PostObjectAsync("auth", new { authKey = _authKey }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
            sessionKey = auth.Value<string>("session");
            if (auth.Value<int>("code") == -1)
            {
                throw new ArgumentException("Wrong MIRAI API HTTP auth key");
            }

            JToken verify = _client.PostObjectAsync("verify", new { sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
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
                    DisplayName = info.Value<string>("name"),
                    Title = info.Value<string>("specialTitle"),
                    Role = GroupRole.Member, // 无法从 api 中得知
                    Group = new Lazy<Group>(() => GetGroupInfoAsync(groupId).GetAwaiter().GetResult())
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
                    Identity = groupId,
                    Name = info.Value<string>("name"),
                };
                group.Members = new Lazy<IEnumerable<Member>>(GetMembersAsync(group).GetAwaiter().GetResult());
                group.Owner = new Lazy<Member>(() => group.Members.Value.FirstOrDefault(x => x.Role == GroupRole.Owner));
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
                    ins.Members = new Lazy<IEnumerable<Member>>(() => GetMembersAsync(ins).GetAwaiter().GetResult());
                    ins.Owner = new Lazy<Member>(() => ins.Members.Value.FirstOrDefault(x => x.Role == GroupRole.Owner));
                    list.Add(ins);
                }
                return list;
            }
            catch
            {
                return Enumerable.Empty<Group>();
            }
        }

        public async Task<MessageChain> GetMessageById(long id)
        {
            try
            {
                JToken message = await (await _client.GetAsync($"/messageFromId?sessionKey={sessionKey}&id={id}")).GetJsonObjectAsync();
                MessageChain chain = _parser.Parse(message["data"].Value<JArray>("messageChain").ToString());
                return chain;
            }catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Member>> GetMembersAsync(Group group)
        {
            try
            {
                JToken members = await (await _client.GetAsync($"memberList?sessionKey={sessionKey}&target={group.Identity}")).GetJsonObjectAsync();
                List<Member> list = new List<Member>();
                foreach (JToken member in members)
                {
                    list.Add(new Member()
                    {
                        Identity = member.Value<long>("id"),
                        DisplayName = member.Value<string>("memberName"),
                        Role = member.Value<string>("permission") switch { "OWNER" => GroupRole.Owner, "ADMINISTRATOR" => GroupRole.Administrator, _ => GroupRole.Member },
                        Group = new Lazy<Group>(group)
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
            HttpResponseMessage response = await _client.PostObjectAsync("sendFriendMessage", new { sessionKey, target = friend.Identity, messageChain = new MessageChain(chain.Where(x => !(x is Quote) && !(x is Source))), quote = ((Quote)chain.FirstOrDefault(x => x is Quote))?.MessageId });
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
            HttpResponseMessage response = await _client.PostObjectAsync("sendGroupMessage", new { sessionKey, target = group.Identity, messageChain = new MessageChain(chain.Where(x => !(x is Quote) && !(x is Source))), quote = ((Quote)chain.FirstOrDefault(x => x is Quote))?.MessageId });
            JToken message = await response.GetJsonObjectAsync();
            if (message.Value<int>("code") != 0)
            {
                throw new Exception(message.Value<string>("msg"));
            }
            return message.Value<long>("messageId");
        }

        public async Task RevokeMessageAsync(long messageId)
        {
            await _client.PostObjectAsync("recall", new { sessionKey, target = messageId });
        }

        public async Task KickMemberAsync(Member member)
        {
            await _client.PostObjectAsync("kick", new { sessionKey, target = member.Group.Value.Identity, member = member.Identity, message = "You are KICKED!" });
        }

        public async Task QuitGroupAsync(Group group)
        {
            await _client.PostObjectAsync("quit", new { sessionKey, target = group.Identity });
        }

        public async Task SendMemberRequestResponsedAsync(long eventId, long userId, long groupId, MemberRequestResponseOperationType action, string message)
        {
            await _client.PostObjectAsync("resp/memberJoinRequestEvent", new { sessionKey, eventId, fromId = userId, groupId, operate = (int)action, message });
        }

        public async Task SendFriendRequestResponsedAsync(long eventId, long userId, long groupId, FriendRequestResponseOperationType action, string message)
        {
            await _client.PostObjectAsync("resp/newFriendRequestEvent", new { sessionKey, eventId, fromId = userId, groupId, operate = (int)action, message });
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
            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(sessionKey), "sessionKey" },
                { new StringContent(type switch { MessageEventType.Friend => "friend", MessageEventType.Group => "group", _ => throw new NotImplementedException() }), "type" }
            };
            using Stream imageStream = image.OpenRead();
            string format = "mirai";
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
                _ = _client.PostObjectAsync("release", new { sessionKey, qq = _selfQQ }).GetAwaiter().GetResult().GetJsonObjectAsync().GetAwaiter().GetResult();
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