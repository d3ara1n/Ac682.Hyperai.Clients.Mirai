using System.Collections.Generic;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using System;
using Newtonsoft.Json;

namespace Ac682.Hyperai.Clients.Mirai.Serialization
{
    public class JsonFormatter : IMessageChainFormatter
    {
        public string Format(MessageChain chain)
        {
            /*
            [
                {
                    "type": "Source",
                    "id": 12345,
                    "time": 12345
                }
            ]
            */
            var list = new LinkedList<object>();
            foreach(var comp in chain)
            {
                list.AddFirst(
                comp switch
                {
                    Plain it => new { type = "Plain", text = it.Text },
                    At it => new { type = "At", id = it.TargetId },
                    Source it =>new  { type = "Source", id = it.MessageId,  time = (DateTime.Now - new DateTime(1970,1,1)).TotalSeconds},
                    _ => throw new NotImplementedException()
                });
            }
            return JsonConvert.SerializeObject(list);
        }
    }
}