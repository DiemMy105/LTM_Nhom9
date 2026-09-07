using System;
using System.Net.Sockets;
using System.Text;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Network;
using ChatTCP.Shared.Utils;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Network
{
    public class ClientConnection
    {
        private TcpClient client;
        private NetworkStream stream;

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public bool IsConnected
        {
            get
            {
                return client != null && client.Connected;
            }
        }

        public string RemoteIp { get; }

        public event Action<Message>? MessageReceived;

        public event Action<ClientConnection>? Disconnected;

        public ClientConnection(TcpClient client)
        {
            this.client = client;
            this.client.ReceiveBufferSize = NetworkConfig.BufferSize;
            this.client.SendBufferSize = NetworkConfig.BufferSize;
            stream = client.GetStream();

            try
            {
                RemoteIp =
                    (client.Client.RemoteEndPoint as System.Net.IPEndPoint)?
                    .Address.ToString()
                    ?? "N/A";
            }
            catch
            {
                RemoteIp = "N/A";
            }
        }

        public void Start()
        {
            try
            {
                int bufSize = NetworkConfig.BufferSize > 0 ? NetworkConfig.BufferSize : 8192;
                byte[] buffer = new byte[bufSize];
                string receivedData = "";

                while (IsConnected)
                {
                    int bytesRead = stream.Read(
                        buffer,
                        0,
                        buffer.Length);

                    if (bytesRead == 0)
                        break;

                    receivedData += Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytesRead);

                    while (receivedData.Contains("\n"))
                    {
                        int index = receivedData.IndexOf("\n");

                        string data =
                            receivedData.Substring(0, index);

                        receivedData =
                            receivedData.Substring(index + 1);

                        if (string.IsNullOrWhiteSpace(data))
                            continue;

                        Message? message =
                            MessageParser.Deserialize(data);

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
                Disconnected?.Invoke(this);
            }
        }

        public void SendMessage(Message message)
        {
            try
            {
                string data =
                    MessageParser.Serialize(message);

                if (string.IsNullOrEmpty(data))
                    return;

                byte[] bytes =
                    Encoding.UTF8.GetBytes(data);

                stream.Write(
                    bytes,
                    0,
                    bytes.Length);
            }
            catch
            {
            }
        }

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