using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    /// <summary> 수신된 패킷을 완성하기 위한 임시 버퍼 </summary>
    public class ReceiveBuffer
    {
        // * Example
        // [][][][r][][][][w][][] Read : 3, Write : 7
        //       [r][][][]        Readable Size   : 4
        //                [w][][] Writable Size   : 3
        private byte[] _buffer;

        private int _readPos;
        private int _writePos;
        private int _bufferSize;

        public ReceiveBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
        }

        /// <summary> 읽을 수 있는 실제 데이터 길이 </summary>
        public int ReadableSize => _writePos - _readPos;

        /// <summary> 새롭게 쓸 수 있는 여유 버퍼 길이 </summary>
        public int WritableSize => _bufferSize - _writePos;

        /// <summary> 읽을 수 있는 실제 데이터 영역 </summary>
        public ArraySegment<byte> ReadableSegment
        {
            get => new ArraySegment<byte>(_buffer, _readPos, ReadableSize);
        }

        /// <summary> 새로운 데이터를 작성할 수 있는 빈 영역 </summary>
        public ArraySegment<byte> WritableSegment
        {
            get => new ArraySegment<byte>(_buffer, _writePos, WritableSize);
        }

        /// <summary> Read, Write 커서를 모두 맨 앞으로 당겨오기 </summary>
        public void Refresh()
        {
            int dataSize = ReadableSize;

            // readPos, writePos가 같은 위치에 있는 경우
            // 잔여 데이터 건들 필요 없이 두 커서만 모두 가장 앞으로 이동
            if (dataSize == 0)
            {
                _readPos = _writePos = 0;
            }
            // 읽을 수 있는 데이터가 존재할 경우
            else
            {
                // _readPos로부터 dataSize만큼의 길이를 시작 위치(Offset)로 복사
                Array.Copy(_buffer, _readPos, _buffer, 0, dataSize);

                // 커서 위치를 앞으로 당겨주기
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        /// <summary> 원하는 크기만큼 읽을 수 있는지 여부 </summary>
        public bool IsReadable(int desiredSize)
        {
            return desiredSize >= ReadableSize;
        }

        /// <summary> 원하는 크기만큼 쓸 수 있는지 여부 </summary>
        public bool IsWritable(int desiredSize)
        {
            return desiredSize >= WritableSize;
        }

        /// <summary> 입력한 길이만큼 Read 커서를 이동시키고, 성공 여부 반환 </summary>
        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > ReadableSize)
                return false;

            _readPos += numOfBytes;
            return true;
        }

        /// <summary> 입력한 길이만큼 Write 커서를 이동시키고, 성공 여부 반환 </summary>
        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > WritableSize)
                return false;

            _writePos += numOfBytes;
            return true;
        }
    }

    public class ReceiveBufferException : Exception
    {
        private readonly string _message;
        public override string Message => _message;

        public ReceiveBufferException(string msg)
        {
            _message = msg;
        }
    }
}
