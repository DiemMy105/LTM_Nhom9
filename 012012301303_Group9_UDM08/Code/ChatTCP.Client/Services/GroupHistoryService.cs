using System;
using System.Text.Json;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Services
{
    public class GroupHistoryService :
        IDisposable
    {
        private readonly TcpClientManager
            tcpClientManager;

        public int CurrentUserId { get; }

        public event Action<GroupHistoryResponse>?
            HistoryReceived;

        public event Action<string>?
            HistoryFailed;

        public GroupHistoryService(
            TcpClientManager tcpClientManager,
            int currentUserId)
        {
            this.tcpClientManager =
                tcpClientManager;

            CurrentUserId = currentUserId;

            this.tcpClientManager.MessageReceived
                += OnMessageReceived;
        }

        public void RequestGroupHistory(
            int groupId,
            int limit = 100)
        {
            EnsureConnected();

            if (groupId <= 0)
            {
                throw new ArgumentException(
                    "Mã nhóm không hợp lệ.");
            }

            GroupHistoryRequest request =
                new GroupHistoryRequest
                {
                    GroupId = groupId,
                    Limit = limit
                };

            Message message = new Message
            {
                SenderId = CurrentUserId,
                GroupId = groupId,

                Type =
                    MessageType.GetChatHistoryRequest,

                Content =
                    JsonSerializer.Serialize(request),

                Timestamp = DateTime.Now
            };

            tcpClientManager.SendMessage(message);
        }

        private void OnMessageReceived(
            Message message)
        {
            if (message.Type !=
                MessageType.GetChatHistoryResponse)
            {
                return;
            }

            try
            {
                GroupHistoryResponse? response =
                    JsonSerializer
                        .Deserialize<GroupHistoryResponse>(
                            message.Content);

                if (response == null)
                {
                    HistoryFailed?.Invoke(
                        "Server trả về lịch sử không hợp lệ.");

                    return;
                }

                if (response.Success)
                {
                    HistoryReceived?.Invoke(response);
                }
                else
                {
                    HistoryFailed?.Invoke(
                        response.Message);
                }
            }
            catch (JsonException)
            {
                HistoryFailed?.Invoke(
                    "Không đọc được lịch sử nhóm từ Server.");
            }
        }

        private void EnsureConnected()
        {
            if (!tcpClientManager.IsConnected)
            {
                throw new InvalidOperationException(
                    "Client chưa kết nối đến Server.");
            }
        }

        public void Dispose()
        {
            tcpClientManager.MessageReceived
                -= OnMessageReceived;
        }
    }
}