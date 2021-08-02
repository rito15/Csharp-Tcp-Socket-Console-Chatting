using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Core
{
    /// <summary> TCP 서버 리스너 </summary>
    public class Listener
    {
        private Socket _listenSocket;
        private Func<Session> _sessionFactory;

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int backlog = 10)
        {
            // 리스너 소켓 생성 및 동작
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(backlog);

            // 사용할 세션 등록
            _sessionFactory = sessionFactory;

            // Accept 시작
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnAcceptCompleted;
            BeginAccept(args);
        }

        /// <summary> 비동기 Accept 시작 </summary>
        private void BeginAccept(SocketAsyncEventArgs args)
        {
            // Accept Socket을 비워놓지 않으면 예외 발생
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);

            // 대기 없이 Accept를 즉시 성공한 경우 처리
            if (!pending)
            {
                OnAcceptCompleted(null, args);
            }
        }

        /// <summary> Accept 완료 처리 </summary>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            // Accept 성공
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory?.Invoke();
                session.Init(args.AcceptSocket);
            }
            // 에러 발생
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            // 처리를 모두 끝낸 후 다시 Accept 시작
            BeginAccept(args);
        }
    }
}
