using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

using System.Net;

namespace Client
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            Utility.PrintClientTitle();

            IPInformation ipInfo = new IPInformation(Dns.GetHostName(), 12345);
            Connector connector = new Connector();

            connector.Connect(ipInfo.EndPoint, () => new ChattingClientSession());

            while (true) ;
        }
    }
}
