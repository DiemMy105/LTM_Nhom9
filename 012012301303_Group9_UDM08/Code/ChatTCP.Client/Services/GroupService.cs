using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Services
{
    public class GroupService : IDisposable
    {
        private readonly TcpClientManager tcpClientManager;

        public int CurrentUserId { get; }

        public event Action<Group>? GroupCreated;
        public event Action<string>? CreateGroupFailed;

        public GroupService(
            TcpClientManager tcpClientManager,
            int currentUserId)
        {
            this.tcpClientManager = tcpClientManager;
            CurrentUserId = currentUserId;

            this.tcpClientManager.MessageReceived += OnMessageReceived;
        }

        public void RequestCreateGroup(
            string groupName,
            IEnumerable<int> memberIds)
        {
            if (!tcpClientManager.IsConnected)
            {
                throw new InvalidOperationException(
                    "Client chưa kết nối đến Server.");
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException(
                    "Tên nhóm không được để trống.");
            }

            CreateGroupRequest request = new CreateGroupRequest
            {
                GroupName = groupName.Trim(),

                MemberIds = memberIds
                    .Where(id => id > 0 && id != CurrentUserId)
                    .Distinct()
                    .ToList()
            };

            Message message = new Message
            {
                SenderId = CurrentUserId,
                Type = MessageType.CreateGroupRequest,
                Content = JsonSerializer.Serialize(request),
                Timestamp = DateTime.Now
            };

            tcpClientManager.SendMessage(message);
        }

        private void OnMessageReceived(Message message)
        {
            if (message.Type != MessageType.CreateGroupResponse)
            {
                return;
            }

            try
            {
                CreateGroupResponse? response =
                    JsonSerializer.Deserialize<CreateGroupResponse>(
                        message.Content);

                if (response == null)
                {
                    CreateGroupFailed?.Invoke(
                        "Server trả về dữ liệu không hợp lệ.");

                    return;
                }

                if (response.Success && response.Group != null)
                {
                    GroupCreated?.Invoke(response.Group);
                }
                else
                {
                    CreateGroupFailed?.Invoke(response.Message);
                }
            }
            catch (JsonException)
            {
                CreateGroupFailed?.Invoke(
                    "Không đọc được kết quả tạo nhóm từ Server.");
            }
        }

        public void Dispose()
        {
            tcpClientManager.MessageReceived -= OnMessageReceived;
        }
    }
}