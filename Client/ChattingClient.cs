using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Core;

namespace Client
{
    class ChattingClient
    {
        public string Name { get; set; }

        private ChattingClientSession _session;
        private bool _isRunning;

        /// <summary> "잘못된 명령어를 입력하셨습니다." </summary>
        private static readonly string WRONG_COMMAND = "잘못된 명령어를 입력하셨습니다.";

        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .
        /// <summary> 채팅 클라이언트 동작 시작 </summary>
        public void Run(ChattingClientSession session)
        {
            _session = session;
            _isRunning = true;

            // 1. 이름 등록
            Console.Write("닉네임을 입력하세요 > ");
            InitMyName(Console.ReadLine());

            // 2. 채팅 시작
            while (_isRunning)
            {
                Console.Write("\n> ");
                string chatting = Console.ReadLine();
                ProcessInput(chatting);
            }
        }

        /// <summary> 종료 </summary>
        public void Quit()
        {
            _isRunning = false;
            Console.WriteLine("채팅이 종료되었습니다.");
        }

        #endregion
        /***********************************************************************
        *                               Private Methods
        ***********************************************************************/
        #region .
        /// <summary> 접속 시 초기 이름 지정 </summary>
        private void InitMyName(string name)
        {
            ChangeMyName(name);
            ChattingPacket packet = new ChattingPacket(name, ChattingCommand.RequestEnterAndName);
            _session.Send(packet.ToByteSegment());
        }

        private void ChangeMyName(string newName)
        {
            Name = newName;
            Console.Title = $"Chatting Client : {newName}";
        }

        /// <summary> 타임스탬프와 함께 콘솔에 출력 </summary>
        private void PrintWithTimeStamp(string msg)
        {
            Console.WriteLine($"{Utility.GetTimeStamp()} {msg}");
        }

        private void PrintCursor()
        {
            Console.Write("\n> ");
        }
        #endregion
        /***********************************************************************
        *                           Processing Client Chattings
        ***********************************************************************/
        #region .
        /// <summary> 닉네임 허용 정규식 </summary>
        private static readonly Regex NickNameRegex = 
            new Regex(@"\/([a-zA-Z]+)\s([a-zA-Z0-9가-힣_]+)$");

        /// <summary> 콘솔로 입력받은 채팅 처리 </summary>
        private void ProcessInput(string chatting)
        {
            if (chatting.Length <= 0) return;

            // 1. 명령인지 검사
            // "/command content123"  꼴
            if (chatting[0] == '/')
            {
                GroupCollection groups = NickNameRegex.Match(chatting).Groups;

                if (groups.Count < 3)
                {
                    Console.WriteLine(WRONG_COMMAND);
                }
                else
                {
                    ProcessCommand(groups[1].Value, groups[2].Value);
                }
            }
            // 2. 채팅 처리
            else
            {
                ProcessChatting(chatting);
            }
        }

        /// <summary> 명령 처리 </summary>
        private void ProcessCommand(string command, string content)
        {
            switch (command)
            {
                // 이름 변경 요청
                case "rename":
                case "Rename":
                    ChattingPacket packet = new ChattingPacket(content, ChattingCommand.RequestRename);
                    _session.Send(packet.ToByteSegment());
                    ChangeMyName(content);
                    break;

                default:
                    Console.WriteLine(WRONG_COMMAND);
                    break;
            }
        }

        /// <summary> 일반 채팅 처리 </summary>
        private void ProcessChatting(string content)
        {
            ChattingPacket packet = new ChattingPacket(content, ChattingCommand.Chat);
            _session.Send(packet.ToByteSegment());
        }
        #endregion
        /***********************************************************************
        *                           Handling Server Packets
        ***********************************************************************/
        #region .
        /// <summary> 서버로부터 전달받은 패킷 데이터 처리 </summary>
        public void HandleDataFromServer(in ChattingPacketData data)
        {
            switch (data.command)
            {
                // 일반 채팅
                case ChattingCommand.Chat:
                    PrintWithTimeStamp(data.content);
                    break;

                // 클라이언트 이름 변경 통지
                case ChattingCommand.NotifyRenamed:
                    string[] names = data.content.Split('|');
                    PrintWithTimeStamp($"닉네임 변경 : {names[0]} -> {names[1]}");
                    break;

                // 클라이언트 입장 통지
                case ChattingCommand.NotifyEntered:
                    PrintWithTimeStamp($"({data.content})님이 입장하셨습니다.");
                    break;

                // 클라이언트 퇴장 통지
                case ChattingCommand.NotifyExit:
                    PrintWithTimeStamp($"({data.content})님이 퇴장하셨습니다.");
                    break;

                default:
                    throw new Exception("Unknown Command");
            }

            PrintCursor();
        }
        #endregion
    }
}
