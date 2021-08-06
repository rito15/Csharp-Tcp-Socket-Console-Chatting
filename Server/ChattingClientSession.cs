using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using Core;

namespace Server
{
    using ByteSegment = System.ArraySegment<byte>;

    class ChattingClientSession : Session
    {
        private ChattingManager _chatManager;
        public string ClientName { get; set; }

        protected override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Conntected To {endPoint}");

            _chatManager = ChattingManager.Instance;
        }

        protected override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"Disconntected From {endPoint}");

            _chatManager.RemoveClient(this);
        }

        protected override int OnReceived(ByteSegment buffer)
        {
            Console.WriteLine($"Received : {buffer.Count}");

            // 1. 패킷 데이터 분리
            ChattingPacketData data = ChattingPacketData.FromByteSegment(buffer);

            // 2. 처리
            _chatManager.HandleDataFromClient(this, data);

            return buffer.Count;
        }

        protected override void OnSent(ByteSegment buffer)
        {
            Console.WriteLine($"Sent : {buffer.Count}\n");
        }
    }
}
