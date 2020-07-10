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
                    case FlashImageMessage fim:
                        return new RemoteFlash(fim.Url);

                    default:
                        throw new NotImplementedException();
                }
            }));

            return chain;
        }

        public static IEnumerable<IMessageBase> ToMessageBases(this MessageChain chain)
        {
            var bases = new List<IMessageBase>();
            foreach(var comp in chain)
            {
                bases.Add((IMessageBase)(
                    comp switch
                    {
                        Source source => new SourceMessage(source.MessageId, DateTime.Now),
                        Plain plain => new PlainMessage(plain.Text),
                        Quote quote => new QuoteMessage(quote.MessageId, 0,0,0,null),
                        At at=> new AtMessage(at.TargetId, at.TargetId.ToString()),
                        AtAll atAll=>new AtAllMessage(),
                        Face face => new FaceMessage(face.FaceId, face.FaceId.ToString()),
                        XmlContent xml => new XmlMessage(xml.Content),
                        JsonContent json => new JsonMessage(json.Content),
                        AppContent app => new AppMessage(app.Content),
                        Poke poke => new PokeMessage((PokeMessage.PokeType)Enum.Parse(typeof(PokeMessage.PokeType), poke.Name.ToString())),
                        Unknown unknown => new UnknownMessage(),
                        RemoteImage image => new ImageMessage(image.ImageId, image.Url, null),
                        LocalImage image => new ImageMessage(image.ImageId,null,image.FilePath),
                        RemoteFlash flash => new FlashImageMessage(flash.ImageId, flash.Url, null),
                        LocalFlash flash => new FlashImageMessage(flash.ImageId, null, flash.FilePath),

                        _ => throw new NotImplementedException()
                    }
                    ));
            }
            return bases;
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
