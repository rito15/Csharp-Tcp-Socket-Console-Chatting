using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Core;
using System.Net;

namespace Server
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            Utility.PrintServerTitle();

            IPInformation ipInfo = new IPInformation(Dns.GetHostName(), 12345);
            Listener listener = new Listener();

            listener.Init(ipInfo.EndPoint, () => new ChattingClientSession());

            while (true) ;
        }
    }
}
