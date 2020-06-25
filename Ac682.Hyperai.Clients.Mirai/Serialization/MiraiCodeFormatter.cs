using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ac682.Hyperai.Clients.Mirai.Serialization
{
    public class MiraiCodeFormatter : IMessageChainFormatter
    {
        public string Format(MessageChain chain)
        {
            var sb = new StringBuilder();
            foreach(var comp in chain)
            {
                sb.Append((string)
                    (comp switch
                    {
                        Plain plain => plain.Text,
                        Source source => $"[mirai:source:{source.MessageId}]",
                        Quote quote => $"[mirai:quote:{quote.MessageId}]",
                        At at => $"[mirai:at:{at.TargetId}]",
                        AtAll atAll => "[mirai:atall]",
                        Face face => $"[mirai:face:{face.FaceId}]",
                        XmlContent xml => $"[mirai:service:60,{xml.Content}]",
                        JsonContent json => $"[mirai:service:1,{json.Content}]",
                        AppContent app => $"[mirai:app:{app.Content}]",
                        Poke poke => $"[mirai:poke:{(int)poke.Name},-1]",

                        ImageBase image => $"[mirai:image:{image.ImageId}]",
                        _ => "[UNSUPPORTED]"
                    }));
                    
            }

            return sb.ToString();
        }
    }
}
