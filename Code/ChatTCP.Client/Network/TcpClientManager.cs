using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Network;
using ChatTCP.Shared.Utils;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Network
{
    public class TcpClientManager
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;
        public bool IsConnected
        {
            get
            {
                return client != null && client.Connected;
            }
        }
        public event Action<Message>? MessageReceived;
        public event Action? Disconnected;

        // Kết nối Client đến Server
        public bool Connect(string ip, int port)
        {
            try
            {
                Disconnect();

                client = new TcpClient();
                client.ReceiveBufferSize = NetworkConfig.BufferSize;
                client.SendBufferSize = NetworkConfig.BufferSize;
                client.SendTimeout = NetworkConfig.Settings.SendTimeoutMs;
                client.ReceiveTimeout = NetworkConfig.Settings.ReceiveTimeoutMs;

                // Kết nối với Timeout cấu hình được
                int timeoutMs = NetworkConfig.ConnectTimeoutMs > 0 ? NetworkConfig.ConnectTimeoutMs : 5000;
                var connectTask = client.ConnectAsync(ip, port);
                if (!connectTask.Wait(timeoutMs))
                {
                    Disconnect();
                    return false;
                }

                if (!client.Connected)
                {
                    Disconnect();
                    return false;
                }

                stream = client.GetStream();
                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }
        // Gửi Message đến Server
        public void SendMessage(Message message)
        {
            if (stream == null)
                return;
            try
            {
                string data = MessageParser.Serialize(message);
                if (string.IsNullOrEmpty(data)) return;

                byte[] bytes = Encoding.UTF8.GetBytes(data);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                // Xử lý lỗi khi gửi
            }
        }
        // Nhận dữ liệu từ Server
        private void ReceiveData()
        {
            if (stream == null)
                return;
            int bufSize = NetworkConfig.BufferSize > 0 ? NetworkConfig.BufferSize : 8192;
            byte[] buffer = new byte[bufSize];
            string receivedData = "";

            try
            {
                while (IsConnected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    receivedData += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    while (receivedData.Contains("\n"))
                    {
                        int index = receivedData.IndexOf("\n");
                        string data = receivedData.Substring(0, index);
                        receivedData = receivedData.Substring(index + 1);
                        if (string.IsNullOrWhiteSpace(data))
                            continue;
                        Message? message = MessageParser.Deserialize(data);
                        if (message != null)
                        {
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                Disconnect();
                Disconnected?.Invoke();
            }
        }
        // Ngắt kết nối
        public void Disconnect()
        {
            try
            {
                stream?.Close();
                client?.Close();
            }
            catch
            {
            }
            stream = null;
            client = null;
        }
    }
}