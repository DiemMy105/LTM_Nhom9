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

        public event Action<List<Group>>? GroupListReceived;
        public event Action<string>? GroupListFailed;

        public GroupService(
            TcpClientManager tcpClientManager,
            int currentUserId)
        {
            this.tcpClientManager = tcpClientManager;
            CurrentUserId = currentUserId;

            this.tcpClientManager.MessageReceived
                += OnMessageReceived;
        }

        public void RequestCreateGroup(
            string groupName,
            IEnumerable<int> memberIds)
        {
            EnsureConnected();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException(
                    "Tên nhóm không được để trống.");
            }

            CreateGroupRequest request =
                new CreateGroupRequest
                {
                    GroupName = groupName.Trim(),

                    MemberIds = memberIds
                        .Where(id =>
                            id > 0 &&
                            id != CurrentUserId)
                        .Distinct()
                        .ToList()
                };

            Message message = new Message
            {
                SenderId = CurrentUserId,

                Type =
                    MessageType.CreateGroupRequest,

                Content =
                    JsonSerializer.Serialize(request),

                Timestamp = DateTime.Now
            };

            tcpClientManager.SendMessage(message);
        }

        public void RequestGroupList()
        {
            EnsureConnected();

            Message message = new Message
            {
                SenderId = CurrentUserId,

                Type =
                    MessageType.GetGroupListRequest,

                Content = string.Empty,

                Timestamp = DateTime.Now
            };

            tcpClientManager.SendMessage(message);
        }

        private void OnMessageReceived(
            Message message)
        {
            switch (message.Type)
            {
                case MessageType.CreateGroupResponse:
                    HandleCreateGroupResponse(message);
                    break;

                case MessageType.GetGroupListResponse:
                    HandleGroupListResponse(message);
                    break;
            }
        }

        private void HandleCreateGroupResponse(
            Message message)
        {
            try
            {
                CreateGroupResponse? response =
                    JsonSerializer
                        .Deserialize<CreateGroupResponse>(
                            message.Content);

                if (response == null)
                {
                    CreateGroupFailed?.Invoke(
                        "Server trả về dữ liệu không hợp lệ.");

                    return;
                }

                if (response.Success &&
                    response.Group != null)
                {
                    GroupCreated?.Invoke(
                        response.Group);
                }
                else
                {
                    CreateGroupFailed?.Invoke(
                        response.Message);
                }
            }
            catch (JsonException)
            {
                CreateGroupFailed?.Invoke(
                    "Không đọc được kết quả tạo nhóm từ Server.");
            }
        }

        private void HandleGroupListResponse(
            Message message)
        {
            try
            {
                List<Group>? groups =
                    JsonSerializer
                        .Deserialize<List<Group>>(
                            message.Content);

                if (groups == null)
                {
                    GroupListFailed?.Invoke(
                        "Server trả về danh sách nhóm không hợp lệ.");

                    return;
                }

                GroupListReceived?.Invoke(groups);
            }
            catch (JsonException)
            {
                GroupListFailed?.Invoke(
                    "Không đọc được danh sách nhóm từ Server.");
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