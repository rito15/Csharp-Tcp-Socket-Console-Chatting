using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using Core;

namespace Client
{
    using ByteSegment = System.ArraySegment<byte>;

    class ChattingServerSession : Session
    {
        private ChattingClient _chatClient;

        protected override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Conntected To {endPoint}");

            // 채팅 클라이언트 생성 및 시작
            _chatClient = new ChattingClient();
            _chatClient.Run(this);
        }

        protected override void OnDisconnected(EndPoint endPoint)
        {
            _chatClient.Quit();
        }

        protected override int OnReceived(ByteSegment buffer)
        {
            // 1. 아직 이름이 지정되지 않은 경우 무시
            if (string.IsNullOrWhiteSpace(_chatClient.Name))
                return buffer.Count;

            // 2. 패킷 데이터 분석
            ChattingPacketData data = ChattingPacketData.FromByteSegment(buffer);

            // 3. 채팅 클라이언트에 전달
            _chatClient.HandleDataFromServer(data);

            return buffer.Count;
        }

        protected override void OnSent(ByteSegment buffer)
        {
            //Console.WriteLine($"Sent : {buffer.Count}\n");
        }
    }
}
