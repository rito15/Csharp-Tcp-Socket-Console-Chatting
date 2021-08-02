using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Core
{
    /// <summary> 클라이언트에서 서버에 TCP 소켓 연결 생성 </summary>
    public class Connector
    {
        private Func<Session> _sessionFactory;

        /// <summary> 서버에 연결 시도하기 </summary>
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory = sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            BeginConenct(args, socket);
        }

        private void BeginConenct(SocketAsyncEventArgs args, Socket socket)
        {
            bool pending = socket.ConnectAsync(args);
            if (pending == false)
            {
                OnConnectCompleted(null, args);
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory?.Invoke();
                session.Init(args.ConnectSocket);

                // Note : Connect()에서 생성한 소켓과 args.ConnectSocket은 동일 객체이다.
            }
            else
            {
                Console.WriteLine($"{nameof(OnConnectCompleted)} Failed : {args.SocketError}");
            }
        }
    }
}
