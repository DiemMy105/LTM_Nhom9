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

        public event Action<List<Group>>?
            GroupListReceived;

        public event Action<string>?
            GroupListFailed;

        public event Action<Message>?
            GroupMessageReceived;

        public event Action<string>?
            GroupMessageFailed;

        public GroupService(
            TcpClientManager tcpClientManager,
            int currentUserId)
        {
            this.tcpClientManager =
                tcpClientManager;

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

        public void SendGroupMessage(
            int groupId,
            string content)
        {
            EnsureConnected();

            if (groupId <= 0)
            {
                throw new ArgumentException(
                    "Mã nhóm không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "Nội dung tin nhắn không được để trống.");
            }

            string normalizedContent =
                content.Trim();

            if (normalizedContent.Length > 4000)
            {
                throw new ArgumentException(
                    "Tin nhắn không được vượt quá 4000 ký tự.");
            }

            Message message = new Message
            {
                SenderId = CurrentUserId,
                ReceiverId = null,
                GroupId = groupId,
                Content = normalizedContent,
                Type = MessageType.GroupChat,
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

                case MessageType.GroupChat:
                    HandleGroupMessage(message);
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

        private void HandleGroupMessage(
            Message message)
        {
            if (!message.GroupId.HasValue ||
                message.GroupId.Value <= 0)
            {
                GroupMessageFailed?.Invoke(
                    "Tin nhắn không có mã nhóm hợp lệ.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                message.Content))
            {
                GroupMessageFailed?.Invoke(
                    "Tin nhắn nhóm không có nội dung.");

                return;
            }

            if (message.SenderId == CurrentUserId)
            {
                return;
            }

            GroupMessageReceived?.Invoke(message);
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