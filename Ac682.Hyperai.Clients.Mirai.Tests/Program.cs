using Hyperai.Events;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ac682.Hyperai.Clients.Mirai.Tests
{
    public class Program
    {
        public static void Main()
        {
            MiraiHttpSession session = new MiraiHttpSession("192.168.1.110", 6259, "NOAUTHKEY", 2594241159);
            session.Connect();
            var watch = new Stopwatch();
            while (true)
            {
                watch.Restart();
                var evt = session.PullEvent();
                if (evt is GroupMessageEventArgs args)
                {
                    if (args.Group.Identity == 594429092)
                    {
                        var builder = new MessageChainBuilder();
                        builder.Add(new Quote(((Source)args.Message.First(x => x is Source)).MessageId));
                        builder.AddPlain("You died");
                        session.SendGroupMessageAsync(args.Group, builder.Build()).Wait();
                    }
                }
                watch.Stop();
                Console.WriteLine("generating event took {0}", watch.ElapsedMilliseconds);
                Thread.Sleep(100);
            }
        }
    }
}