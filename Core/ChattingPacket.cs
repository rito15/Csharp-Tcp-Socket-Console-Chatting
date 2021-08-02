using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    using ByteSegment = ArraySegment<byte>;

    public class ChattingPacket : Packet
    {
        public readonly ushort command; // 명령어
        public readonly string content; // 채팅 내용

        public ChattingPacket(string content, ChattingCommand command = ChattingCommand.Chat)
        {
            this.command = (ushort)command;
            this.content = content;

            this.size = 0;
        }

        public ByteSegment ToByteSegment()
        {
            // * ID는 일단 필요 없으니 사용하지 않음

            // 1. Command & Content 
            byte[] command = BitConverter.GetBytes(this.command);
            byte[] content = Encoding.UTF8.GetBytes(this.content);

            this.size += sizeof(ushort) * 2;
            this.size += (ushort)content.Length;

            // 2. Size
            byte[] size = BitConverter.GetBytes(this.size);

            // 3. Send Buffer에 작성
            SendBuffer.Factory.Write(size);
            SendBuffer.Factory.Write(command);
            SendBuffer.Factory.Write(content);

            return SendBuffer.Factory.Read();
        }
    }

    /// <summary> 간소화된 채팅 패킷 데이터 </summary>
    public readonly struct ChattingPacketData
    {
        public readonly ChattingCommand command;
        public readonly string content;

        private ChattingPacketData(ChattingCommand command, string content)
        {
            this.command = command;
            this.content = content;
        }

        /// <summary> ByteSegment로부터 패킷 데이터 조립 </summary>
        public static ChattingPacketData FromByteSegment(ByteSegment seg)
        {
            ushort usSize = BitConverter.ToUInt16(seg.Array, 0);
            ushort usCommand = BitConverter.ToUInt16(seg.Array, 2);

            int contentLen = usSize - 4;
            ChattingCommand command = (ChattingCommand)usCommand;
            string content = Encoding.UTF8.GetString(seg.Array, 4, contentLen);

            return new ChattingPacketData(command, content);
        }
    }

    /// <summary> 채팅 명령어 </summary>
    public enum ChattingCommand
    {
        /* [1] 공통 */
        /// <summary> 일반 채팅 </summary>
        Chat,

        /* [2] 클라이언트 -> 서버 */
        /// <summary> 닉네임 변경 요청 </summary>
        RequestRename,
        /// <summary> 입장 + 닉네임 지정 </summary>
        RequestEnterAndName,
        /// <summary> 퇴장 </summary>
        RequestExit,

        /* [3] 서버 -> 클라이언트 */
        /// <summary> 닉네임 변경사항 통지 </summary>
        NotifyRenamed,
        /// <summary> 입장 통지 </summary>
        NotifyEntered,
        /// <summary> 퇴장 통지 </summary>
        NotifyExit,
    }
}
