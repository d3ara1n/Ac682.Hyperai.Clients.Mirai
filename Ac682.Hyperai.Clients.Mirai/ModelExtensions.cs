using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Numerics;
using System.Text;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Hyperai.Relations;
using Mirai_CSharp.Models;

namespace Ac682.Hyperai.Clients.Mirai
{
    public static class ModelExtensions
    {
        public static MessageChain ToMessageChain(this IEnumerable<IMessageBase> messages)
        {
            var chain = new MessageChain(messages.Select<IMessageBase,MessageComponent>(x =>
            {
                switch(x)
                {
                    case SourceMessage sm:
                        var source = new Source(sm.Id);
                        return source;
                    case PlainMessage pm:
                        var plain = new Plain(pm.Message);
                        return plain;
                    case QuoteMessage qm:
                        var quote = new Quote(qm.Id);
                        return quote;
                    case AtMessage am:
                        var at = new At(am.Target);
                        return at;
                    case AtAllMessage aam:
                        return new AtAll();
                    case FaceMessage fm:
                        return new Face(fm.Id);
                    case XmlMessage xm:
                        return new XmlContent(xm.Xml);
                    case JsonMessage jm:
                        return new JsonContent(jm.Json);
                    case AppMessage apm:
                        return new AppContent(apm.Content);
                    case PokeMessage pm:
                        return new Poke((PokeType)Enum.Parse(typeof(PokeType), pm.Name.ToString()));
                    case UnknownMessage um:
                        return new Unknown(um.Data.GetRawText());
                    case ImageMessage im:
                        return new RemoteImage(im.ImageId, im.Url);

                    default:
                        throw new NotImplementedException();
                }
            }));

            return chain;
        }
        public static Friend ToFriend(this IFriendInfo info)
        {
            var friend = new Friend()
            {
                Identity = info.Id,
                Nickname = info.Name,
                Remark = info.Remark
            };
            return friend;
        }
    }
}
