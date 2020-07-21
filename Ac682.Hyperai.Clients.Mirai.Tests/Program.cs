using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace Ac682.Hyperai.Clients.Mirai.Tests
{
    public class Program
    {
        public static void Main()
        {
            MiraiHttpSession session = new MiraiHttpSession("192.168.1.110", 6259, "NONKEYoMzCUkbhSLF4J", 2594241159);
            session.Connect();
            var watch = new Stopwatch();
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
