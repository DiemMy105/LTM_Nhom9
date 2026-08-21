using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ChatTCP.Shared.Models;

namespace ChatTCP.Server.Network
{
    public class TcpServer
    {
        private TcpListener? listener;
        private Thread? serverThread;
        public bool IsRunning { get; private set; }
        // khi có Client gửi Message
        public event Action<ClientConnection, Message>? MessageReceived;
        // khi có Client kết nối
        public event Action<ClientConnection>? ClientConnected;
        // Khởi động Server
        public bool Start(int port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                IsRunning = true;
                serverThread = new Thread(AcceptClients);
                serverThread.IsBackground = true;
                serverThread.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }
        // Chờ và nhận Client kết nối
        private void AcceptClients()
        {
            if (listener == null)
                return;
            try
            {
                while (IsRunning)
                {
                    // Chờ Client kết nối
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    // Tạo đối tượng quản lý Client
                    ClientConnection clientConnection =
                        new ClientConnection(tcpClient);
                    // Báo khi có Client gửi Message
                    clientConnection.MessageReceived +=
                        (message) =>
                        {
                            MessageReceived?.Invoke(
                                clientConnection,
                                message
                            );
                        };
                    // Thông báo có Client kết nối
                    ClientConnected?.Invoke(clientConnection);

                    // Tạo Thread riêng cho Client này
                    Thread clientThread =
                        new Thread(clientConnection.Start);

                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch
            {
                // Server đã dừng hoặc có lỗi
            }
        }
        // Dừng Server
        public void Stop()
        {
            try
            {
                IsRunning = false;
                listener?.Stop();
            }
            catch
            {
            }
            listener = null;
        }
    }
}