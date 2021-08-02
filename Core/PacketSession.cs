using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    using ByteSegment = System.ArraySegment<byte>;

    /// <summary> 패킷을 사용하는 세션 </summary>
    public abstract class PacketSession : Session
    {
        /// <summary> 패킷 헤더 길이 </summary>
        public static readonly ushort HeaderSize = 2;

        protected sealed override int OnReceived(ByteSegment buffer)
        {
            // 처리한 데이터 길이
            int processedLen = 0;

            while (true)
            {
                // 1. 헤더 파싱조차 불가능하게 작은 데이터가 온 경우, 처리 X
                if (buffer.Count < HeaderSize)
                    break;

                // 헤더를 확인하여 패킷이 완전히 도착했는지 여부 확인
                ushort dataLen = BitConverter.ToUInt16(buffer.Array, buffer.Offset);

                // 2. 아직 완전한 패킷이 도착한 것이 아닌 경우, 처리 X
                if (buffer.Count < dataLen)
                    break;

                // 3. 완전한 패킷 처리
                OnReceivePacket(new ByteSegment(buffer.Array, buffer.Offset, dataLen));
                processedLen += dataLen;

                // 4. 다음 패킷 확인(Offset 이동)
                buffer = new ByteSegment(buffer.Array, buffer.Offset + dataLen, buffer.Count - dataLen);
            }

            return processedLen;
        }

        /// <summary> 완전한 하나의 패킷 처리 </summary>
        protected abstract void OnReceivePacket(ByteSegment buffer);


        /// <summary> 기본 패킷 정보 읽기(임시) </summary>
        protected string GetPacketInfo(ByteSegment buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + sizeof(ushort));

            return $"Size : {size}, ID : {id}";
        }

        /// <summary> 기본 패킷 전송하기(임시) </summary>
        public void SendPacket(Packet packet)
        {
            byte[] size = BitConverter.GetBytes(packet.size);
            byte[] id = BitConverter.GetBytes(packet.id);
            SendBuffer.Factory.Write(size, id);
            Send(SendBuffer.Factory.Read());
        }
    }
}
