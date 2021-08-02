using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Core
{
    public class IPInformation
    {
        public IPAddress Address { get; private set; }
        public AddressFamily AddressFamily { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

        public IPInformation(string hostNameOrAddress, int portNumber)
        {
            IPHostEntry ipHost = Dns.GetHostEntry(hostNameOrAddress);

            // 호스트가 보유한 IP 주소 중 첫 번째를 가져온다.
            Address = ipHost.AddressList[0];

            // IP 주소와 포트 번호를 통해 IP 연결 말단 객체를 생성한다.
            EndPoint = new IPEndPoint(Address, portNumber);

            AddressFamily = EndPoint.AddressFamily;
        }
    }
}
