using System;
using System.Net.Sockets;
using System.Text;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Network;

namespace ChatTCP.Server.Network
{
    public class ClientConnection
    {
        private TcpClient client;
        private NetworkStream stream;
        public int UserId { get; set; }
        public bool IsConnected
        {
            get
            {
                return client != null && client.Connected;
            }
        }
        //khi nhận được Message
        public event Action<Message>? MessageReceived;
        public ClientConnection(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
        }
        // Bắt đầu nhận dữ liệu từ Client
        public void Start()
        {
            try
            {
                byte[] buffer = new byte[4096];
                string receivedData = "";
                while (IsConnected)
                {
                    int bytesRead = stream.Read(
                        buffer,
                        0,
                        buffer.Length
                    );

                    if (bytesRead == 0)
                        break;

                    receivedData += Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead
                    );
                    // Kiểm tra đã nhận đủ Message chưa
                    while (receivedData.Contains("\n"))
                    {
                        int index = receivedData.IndexOf("\n");
                        string data = receivedData.Substring(0, index);
                        receivedData = receivedData.Substring(index + 1);
                        if (string.IsNullOrWhiteSpace(data))
                            continue;
                        Message message =
                            MessageParser.Deserialize(data);

                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch
            {
                // Client ngắt kết nối
            }
        }
        // Gửi Message đến Client
        public void SendMessage(Message message)
        {
            try
            {
                string data = MessageParser.Serialize(message);
                data += "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                // Xử lý lỗi gửi
            }
        }
        // Đóng kết nối
        public void Disconnect()
        {
            try
            {
                stream.Close();
                client.Close();
            }
            catch
            {
            }
        }
    }
}