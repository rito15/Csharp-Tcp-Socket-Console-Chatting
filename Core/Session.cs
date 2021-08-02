using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Core
{
    using ByteSegment = System.ArraySegment<byte>;

    public abstract class Session
    {
        private const int TRUE = 1;
        private const int FALSE = 0;

        private Socket _socket;
        private int _isConnected;

        // Sending Fields
        private SocketAsyncEventArgs _sendArgs;
        private Queue<ByteSegment> _sendQueue;     // 동시 전송 방지를 위한 큐
        private List<ByteSegment> _sendBufferList; // 묶음 전송을 위한 리스트
        private object _sendLock;

        // Receiving Fields
        private SocketAsyncEventArgs _recvArgs;
        private ReceiveBuffer _recvBuffer;

        // Event Handlers
        protected abstract void OnConnected(EndPoint endPoint);
        protected abstract void OnDisconnected(EndPoint endPoint);
        protected abstract int OnReceived(ByteSegment buffer);
        protected abstract void OnSent(ByteSegment buffer);

        public Session()
        {
            _isConnected = FALSE;

            _sendLock = new object();
            _sendQueue = new Queue<ByteSegment>(8);
            _sendBufferList = new List<ByteSegment>(8);

            _recvBuffer = new ReceiveBuffer(1024);
        }
        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .

        public void Init(Socket socket)
        {
            _socket = socket;
            _isConnected = TRUE;

            // Receive
            _recvArgs = new SocketAsyncEventArgs();
            _recvArgs.Completed += OnReceiveCompleted;
            //_recvArgs.SetBuffer(new byte[1024], 0, 1024);

            BeginReceive();

            // Send
            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.Completed += OnSendCompleted;

            // 연결 완료 통보하기
            // 반드시 Init 끝자락에서 호출
            OnConnected(socket.RemoteEndPoint);
        }

        /// <summary> 대상 소켓과의 연결 종료하기 </summary>
        public void Disconnect()
        {
            // 이미 연결이 끊긴 경우 확인
            if (Interlocked.Exchange(ref _isConnected, FALSE) == FALSE)
                return;

            OnDisconnected(_socket.RemoteEndPoint);

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        /// <summary> 연결된 대상 소켓에 데이터 전송하기 </summary>
        public void Send(ByteSegment sendBuffer)
        {
            lock (_sendLock)
            {
                _sendQueue.Enqueue(sendBuffer);

                // Send를 수행 중인 스레드가 없을 경우, Send 수행
                if (_sendBufferList.Count == 0)
                    BeginSend();
            }
        }

        /// <summary> UTF-8 인코딩으로 메시지 전송하기 </summary>
        public void SendUTF8String(string message)
        {
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);
            Send(new ByteSegment(sendBuffer, 0, sendBuffer.Length));
        }
        #endregion
        /***********************************************************************
        *                               Send Methods
        ***********************************************************************/
        #region .
        private void BeginSend()
        {
            // 1. Send Queue -> Buffer List에 모두 옮겨 담기
            //_sendBufferList.Clear(); -> OnSendCompleted()에서 호출
            while (_sendQueue.Count > 0)
            {
                ByteSegment buffer = _sendQueue.Dequeue();
                _sendBufferList.Add(buffer);
            }
            _sendArgs.BufferList = _sendBufferList;

            // 2. Send 수행
            bool pending = true;
            try
            {
                pending = _socket.SendAsync(_sendArgs);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine($"대상이 연결을 강제로 종료하였습니다.");
                Disconnect();
            }

            if (pending == false)
            {
                // 즉시 수행되는 경우
                OnSendCompleted(null, _sendArgs);
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_sendLock)
            {
                int byteTransferred = args.BytesTransferred;

                if (byteTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        foreach (var buffer in _sendBufferList)
                        {
                            OnSent(buffer);
                        }

                        // 버퍼 리스트 비워주기(Send 수행 종료를 알리는 것과 상통)
                        _sendBufferList.Clear();

                        // 큐에 버퍼가 더 남아있으면 Send 이어서 수행
                        if (_sendQueue.Count > 0)
                        {
                            Console.WriteLine($"QUEUE IS NOT EMPTY : {_sendQueue.Count}");
                            BeginSend();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{nameof(OnSendCompleted)}() Error : {e}");
                    }
                }
                else
                {
                    string msg = $"{nameof(OnSendCompleted)}() Error : "
                        + $"Byte Transferred [{byteTransferred}], "
                        + $"Error Type [{args.SocketError}]\n";
                    Console.WriteLine(msg);

                    Disconnect(); // 소켓 에러 발생 시 세션 종료
                }
            }
        }
        #endregion
        /***********************************************************************
        *                               Receive Methods
        ***********************************************************************/
        #region .
        // NOTE : Receive는 한 번의 수신이 완료되어야만 다음 수신을 준비하므로
        //        스레드 동기화 필요 X
        private void BeginReceive()
        {
            // 1. Receive Buffer의 여유 공간 참조
            _recvBuffer.Refresh();
            ByteSegment segment = _recvBuffer.WritableSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            // 2. Receive 수행
            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
            {
                // 즉시 수행되는 경우
                OnReceiveCompleted(null, _recvArgs);
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            int byteTransferred = args.BytesTransferred;

            if (byteTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // 1. Receive Buffer의 Write 커서 이동
                    if (_recvBuffer.OnWrite(byteTransferred) == false)
                    {
                        throw new ReceiveBufferException($"버퍼에 쓸 수 있는 잔여 공간이 없습니다 - " +
                            $"Writable Size : {_recvBuffer.WritableSize}, Byte Transferred : {byteTransferred}");
                    }

                    // 2. 컨텐츠 쪽에 데이터를 넘겨주고, 처리된 데이터 길이 반환받기
                    // OnReceived() 메소드에서 패킷을 분석하여, 불완전한 패킷인 경우 0을 반환한다.
                    int processedLen = OnReceived(_recvBuffer.ReadableSegment);
                    if (processedLen < 0 || processedLen > _recvBuffer.ReadableSize)
                    {
                        throw new ReceiveBufferException($"버퍼를 읽는 데 실패하였습니다 - " +
                            $"Readable Size : {_recvBuffer.ReadableSize}, 읽으려는 길이 : {processedLen}");
                    }

                    // 3. 처리된 데이터 길이만큼 Receive Buffer의 Read 커서 이동
                    if (_recvBuffer.OnRead(processedLen) == false)
                    {
                        throw new ReceiveBufferException($"버퍼에서 읽을 수 있는 데이터 길이보다 입력한 길이가 더 큽니다 - " +
                            $"Readable Size : {_recvBuffer.ReadableSize}, 읽으려는 길이 : {processedLen}");
                    }

                    // Receive 재시작
                    BeginReceive();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{nameof(OnReceiveCompleted)}() Error : {e}");
                    Disconnect();
                }
            }
            else
            {
                string msg = $"{nameof(OnReceiveCompleted)}() Error : "
                        + $"Byte Transferred [{byteTransferred}], "
                        + $"Error Type [{args.SocketError}]\n";
                Console.WriteLine(msg);

                Disconnect(); // 소켓 에러 발생 시 세션 종료
            }
        }
        #endregion

    }
}
