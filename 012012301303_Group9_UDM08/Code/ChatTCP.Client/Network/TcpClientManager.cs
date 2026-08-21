using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Network;
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
        // Sự kiện khi nhận được Message
        public event Action<Message>? MessageReceived;

        // Kết nối Client đến Server
        public bool Connect(string ip, int port)
        {
            try
            {
                client = new TcpClient();
                client.Connect(ip, port);
                stream = client.GetStream();
                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                return true;
            }
            catch
            {
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
                // Serialize đã tự động thêm \n ở cuối
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
            byte[] buffer = new byte[4096];
            string receivedData = "";

            try
            {
                while (IsConnected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    receivedData += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Đọc từng dòng JSON dựa vào ký tự \n
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
                // Server ngắt kết nối
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