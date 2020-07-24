using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using Hyperai.Relations;
using Hyperai.Messages;
using Hyperai.Messages.ConcreteModels;

namespace Ac682.Hyperai.Clients.Mirai.Tests
{
    public class Program
    {
        public static void Main()
        {
            MiraiHttpSession session = new MiraiHttpSession("192.168.1.110", 6259, "NOAUTHKEY", 2594241159);
            session.Connect();
            var watch = new Stopwatch();
            
            session.SendGroupMessageAsync(new Group() { Identity = 594429092 }, new MessageChain(new MessageComponent[] { new Image("", new Uri(@"https://u.iheit.com/images/2020/07/24/DOAX-VenusVacation_200119_132834.jpg")), new Plain("DOA") })).Wait();
            while (true)
            {
                watch.Restart();
                session.PullEvent();
                watch.Stop();
                Console.WriteLine("generating event took {0}", watch.ElapsedMilliseconds);
                Thread.Sleep(100);
            }

        }
    }
}
