using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Core;

namespace Server
{
    using ByteSegment = System.ArraySegment<byte>;

    /// <summary> 채팅 기능 관리 </summary>
    class ChattingManager
    {
        // Singleton
        public static ChattingManager Instance => _instance;
        private static readonly ChattingManager _instance = new ChattingManager();
        private ChattingManager() { } // 생성자 봉인

        public HashSet<ChattingServerSession> ClientSessionList { get; } = new HashSet<ChattingServerSession>(4);
        private readonly object _lock = new object();

        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .
        /// <summary> 클라이언트로부터 전달받은 패킷 데이터 처리 </summary>
        public void HandleDataFromClient(ChattingServerSession clientSession, in ChattingPacketData data)
        {
            Console.Write(Utility.GetTimeStamp() + " ");

            switch (data.command)
            {
                // 채팅 메시지 전달
                case ChattingCommand.Chat:
                    string chatContent = $"[{clientSession.ClientName}] : {data.content}";
                    Console.WriteLine($"Handle - Chat : {chatContent}");
                    RelayChattingMessage(clientSession, chatContent);
                    break;

                // 이름 변경 요청
                case ChattingCommand.RequestRename:
                    Console.WriteLine($"Handle - Request Rename : {clientSession.ClientName} -> {data.content}");
                    RenameClient(clientSession, data.content);
                    break;

                // 접속 및 이름 지정 요청
                case ChattingCommand.RequestEnterAndName:
                    Console.WriteLine($"Handle - Enter and Name : {data.content}");
                    AddClient(clientSession, data.content);
                    break;

                default:
                    break;
            }
        }
        #endregion
        /***********************************************************************
        *                               Processing Methods
        ***********************************************************************/
        #region .
        /// <summary> 새로운 클라이언트를 목록에 추가 </summary>
        private void AddClient(ChattingServerSession clientSession, string name)
        {
            bool addSucceeded;

            // 1. 목록에 추가
            lock (_lock)
            {
                addSucceeded = ClientSessionList.Add(clientSession);
            }

            // 2. 이름 지정
            clientSession.ClientName = name;

            if (!addSucceeded)
                return;

            // 3. 다른 클라이언트들에 입장 통지
            ChattingPacket packet = new ChattingPacket(name, ChattingCommand.NotifyEntered);
            BroadcastToAll(packet);
        }

        /// <summary> 목록에서 클라이언트 제거 </summary>
        public void RemoveClient(ChattingServerSession clientSession)
        {
            // 1. 목록에서 제거
            lock (_lock)
            {
                ClientSessionList.Remove(clientSession);
            }

            // 2. 이름 지정 여부 확인
            if (string.IsNullOrWhiteSpace(clientSession.ClientName))
                return;

            // 3. 다른 클라이언트들에 퇴장 통지
            if (ClientSessionList.Count > 0)
            {
                ChattingPacket packet = new ChattingPacket(clientSession.ClientName, ChattingCommand.NotifyExit);
                MulticastToAll(clientSession, packet);
            }
        }

        /// <summary> 지정한 클라이언트의 이름 변경 </summary>
        private void RenameClient(ChattingServerSession clientSession, string newName)
        {
            // 1. "기존이름|새로운이름" 꼴로 내용 구성
            string renameContent = $"{clientSession.ClientName}|{newName}";

            // 2. 이름 변경
            clientSession.ClientName = newName;

            // 3. 모든 클라이언트들에 이름 변경 통지
            ChattingPacket packet = new ChattingPacket(renameContent, ChattingCommand.NotifyRenamed);
            BroadcastToAll(packet);
        }

        /// <summary> 해당 클라이언트를 제외한 모두에게 채팅 메시지 전달 </summary>
        private void RelayChattingMessage(ChattingServerSession clientSession, string message)
        {
            if (ClientSessionList.Count > 1)
            {
                ChattingPacket packet = new ChattingPacket(message, ChattingCommand.Chat);
                MulticastToAll(clientSession, packet);
            }
        }

        #endregion
        /***********************************************************************
        *                               Cast Methods
        ***********************************************************************/
        #region .
        /// <summary> 모든 클라이언트들에 패킷 전달 </summary>
        private void BroadcastToAll(ChattingPacket packet)
        {
            ByteSegment bPacket = packet.ToByteSegment();

            lock (_lock)
            {
                foreach (var client in ClientSessionList)
                {
                    client.Send(bPacket);
                }
            }
        }

        /// <summary> 패킷을 특정 클라이언트 제외, 다른 클라이언트 세션들에 모두 전달 </summary>
        private void MulticastToAll(Session except, ChattingPacket packet)
        {
            ByteSegment bPacket = packet.ToByteSegment();

            lock (_lock)
            {
                foreach (var client in ClientSessionList)
                {
                    if (client == except) continue;

                    client.Send(bPacket);
                }
            }
        }
        #endregion
    }
}
