using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Ac682.Hyperai.Clients.Mirai.Serialization
{
    public class JsonParser : IMessageChainParser
    {
        public MessageChain Parse(string text)
        {
            var array = JsonConvert.DeserializeObject<JArray>(text);
            var builder = new MessageChainBuilder();
            foreach (var it in array)
            {
                builder.Add(it.Value<string>("type") switch
                {
                    "Source" => new Source(it.Value<int>("id")),
                    "Plain" => new Plain(it.Value<string>("text")),
                    "Face" => new Face(it.Value<int>("faceId")),
                    "Quote" => new Quote(it.Value<int>("id")),
                    "AtAll" => new AtAll(),
                    "At" => new At(it.Value<long>("target")),
                    "Image" => new Image(it.Value<string>("imageId"), new Uri(it.Value<string>("url") ?? it.Value<string>("path"))),
                    _ => throw new NotImplementedException("MessageComponent type not supported: " + it.Value<string>("type"))
                });
            }
            return builder.Build();
        }
    }
}