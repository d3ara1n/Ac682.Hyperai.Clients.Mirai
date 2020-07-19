using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ac682.Hyperai.Clients.Mirai.Serialization
{
    public class JsonFormatter : IMessageChainFormatter
    {
        public string Format(MessageChain chain)
        {
            LinkedList<object> list = new LinkedList<object>();
            foreach (MessageComponent comp in chain)
            {
                list.AddLast(
                comp switch
                {
                    Plain it => new { type = "Plain", text = it.Text },
                    At it => new { type = "At", target = it.TargetId },
                    Source it => new { type = "Source", id = it.MessageId, time = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds },
                    Face it => new { type = "Face", faceId = it.FaceId },
                    Quote it => new { type = "Quote", id = it.MessageId },
                    AtAll it => new { type = "AtAll" },
                    Image it => new { type = "Image", url = it.Url.AbsoluteUri },
                    Flash it => new { type = "FlashImage", url = it.Url.AbsoluteUri },
                    AppContent it => new { type = "App", content = it.Content },
                    JsonContent it => new { type = "Json", content = it.Content },
                    XmlContent it => new { type = "Xml", content = it.Content },
                    _ => throw new NotImplementedException("MessageComponent type not supported: " + comp.TypeName)
                });
            }
            return JsonConvert.SerializeObject(list);
        }
    }
}