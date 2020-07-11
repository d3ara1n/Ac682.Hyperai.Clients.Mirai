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
            var list = new LinkedList<object>();
            foreach (var comp in chain)
            {
                list.AddFirst(
                comp switch
                {
                    Plain it => new { type = "Plain", text = it.Text },
                    At it => new { type = "At", target = it.TargetId },
                    Source it => new { type = "Source", id = it.MessageId, time = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds },
                    Face it => new { type = "Face", faceId = it.FaceId },
                    Quote it => new { type = "Quote", id = it.MessageId },
                    AtAll it => new { type = "AtAll" },
                    Image it => new { type = "Image", url = it.IsRemote ? it.Url.AbsoluteUri : null, path = it.IsRemote ? null : it.Url.LocalPath },
                    _ => throw new NotImplementedException("MessageComponent type not supported: " + comp.TypeName)
                });
            }
            return JsonConvert.SerializeObject(list);
        }
    }
}