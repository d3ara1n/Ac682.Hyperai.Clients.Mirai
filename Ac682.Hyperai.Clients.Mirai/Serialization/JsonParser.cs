using System.Collections.Generic;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using System;
using Newtonsoft.Json;

namespace Ac682.Hyperai.Clients.Mirai.Serialization
{
    public class JsonParser : IMessageChainParser
    {
        public MessageChain Parse(string text)
        {
            var list = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(text);
            var builder = new MessageChainBuilder();
            foreach(var it in list)
            {
                builder.Add(it.type.ToLower() switch
                {
                    "source" => new Source(it.id),
                    "plain" => new Plain(it.text),
                    _ => throw new NotImplementedException()
                });
            }
            return builder.Build();
        }
    }
}