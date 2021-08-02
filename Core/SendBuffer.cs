using System;
using System.Collections.Generic;
using System.Threading;

namespace Core
{
    using ByteSegment = System.ArraySegment<byte>;

    /// <summary> 전송 시 패킷을 조립하기 위한 임시 버퍼 </summary>
    public class SendBuffer
    {
        /// <summary> Send Buffer를 TLS로 간편히 제공하기 위한 정적 클래스 </summary>
        public static class Factory
        {
            public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => null);

            public static int ChunkSize { get; set; } = 4096 * 100;

            /// <summary> 버퍼에 새로운 데이터 작성하기 </summary>
            public static void Write(byte[] data)
            {
                // 초기 접근 시 버퍼 새로 생성
                if (CurrentBuffer.Value == null)
                    CurrentBuffer.Value = new SendBuffer(ChunkSize);

                // 여유 공간이 없는 경우 버퍼 새로 생성
                if (CurrentBuffer.Value.CheckWritableSize(data.Length) == false)
                    CurrentBuffer.Value = new SendBuffer(ChunkSize);

                // 버퍼에 쓰기
                CurrentBuffer.Value.Write(data);
            }

            public static void Write(params byte[][] data)
            {
                foreach (var item in data)
                {
                    Write(item);
                }
            }

            /// <summary> 버퍼에서 읽을 수 있는 모든 데이터 읽어오기 </summary>
            public static ByteSegment Read()
            {
                if (CurrentBuffer.Value == null)
                    throw new InvalidOperationException($"Read 이전에 Write를 먼저 수행해야 합니다.");

                return CurrentBuffer.Value.Read();
            }
        }

        // [][][r][][][][w][][][]
        private readonly byte[] _buffer;
        private int _readPos;
        private int _writePos;

        /// <summary> 데이터를 새롭게 추가할 수 있는 여유 공간 </summary>
        public int WritableSize => _buffer.Length - _writePos;

        /// <summary> 데이터를 읽을 수 있는 길이 </summary>
        public int ReadableSize => _writePos - _readPos;

        public SendBuffer(int bufferSize)
        {
            _buffer = new byte[bufferSize];
            _readPos = _writePos = 0;
        }

        /// <summary> 해당 길이만큼 버퍼에 쓸 수 있는지 검사 </summary>
        public bool CheckWritableSize(int len)
        {
            return WritableSize >= len;
        }

        /// <summary> Send Buffer에 새로운 데이터 작성하기 </summary>
        public void Write(byte[] data)
        {
            int len = data.Length;
            if (len > WritableSize)
                throw new ArgumentOutOfRangeException($"Send Buffer에 쓰려는 데이터의 길이({len})가" +
                    $" 버퍼의 여유 길이({_buffer.Length})보다 큽니다.");

            // Write Pos부터 len 길이만큼 버퍼에 쓰기
            Array.Copy(data, 0, _buffer, _writePos, len);

            // Write Pos 이동
            _writePos += len;
        }

        /// <summary> 버퍼에 가장 최근에 작성된 데이터 모두 읽어오기 </summary>
        public ByteSegment Read()
        {
            if (ReadableSize <= 0)
                throw new IndexOutOfRangeException($"Send Buffer에서 읽을 수 있는 데이터가 없습니다." +
                    $" (Read Pos : {_readPos}, Write Pos : {_writePos})");

            // 이전의 데이터 캐싱
            int readPos = _readPos;
            int readableSize = ReadableSize;

            // Read Pos 이동
            _readPos = _writePos;

            return new ByteSegment(_buffer, readPos, readableSize);
        }
    }
}
