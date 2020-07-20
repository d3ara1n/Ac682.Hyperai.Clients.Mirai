using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ac682.Hyperai.Clients.Mirai.Tests
{
    public class Program
    {
        public static void Main()
        {
            MiraiHttpSession session = new MiraiHttpSession("192.168.1.110", 6259, "NONKEYoMzCUkbhSLF4J", 2594241159);
            session.Connect();
            while (true)
            {
                session.PullEvent();
                Thread.Sleep(100);
            }

        }
    }
}
