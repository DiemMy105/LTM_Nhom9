using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Services
{
    public class GroupHistoryService
    {
        private readonly GroupManager groupManager;

        private readonly object historyLock =
            new object();

        private readonly Dictionary<int, List<Message>>
            histories =
                new Dictionary<int, List<Message>>();

        private int nextMessageId = 1;

        public GroupHistoryService(
            GroupManager groupManager)
        {
            this.groupManager = groupManager;
        }

        public Message SaveGroupMessage(
            Message groupMessage)
        {
            if (groupMessage.Type !=
                MessageType.GroupChat)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải tin nhắn nhóm.");
            }

            if (!groupMessage.GroupId.HasValue ||
                groupMessage.GroupId.Value <= 0)
            {
                throw new ArgumentException(
                    "Mã nhóm không hợp lệ.");
            }

            if (groupMessage.SenderId <= 0)
            {
                throw new ArgumentException(
                    "Người gửi không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(
                groupMessage.Content))
            {
                throw new ArgumentException(
                    "Nội dung tin nhắn không được để trống.");
            }

            int groupId =
                groupMessage.GroupId.Value;

            if (!groupManager.IsMember(
                groupId,
                groupMessage.SenderId))
            {
                throw new UnauthorizedAccessException(
                    "Người gửi không thuộc nhóm.");
            }

            Message storedMessage =
                CloneMessage(groupMessage);

            lock (historyLock)
            {
                storedMessage.Id = nextMessageId;
                nextMessageId++;

                if (!histories.ContainsKey(groupId))
                {
                    histories[groupId] =
                        new List<Message>();
                }

                histories[groupId]
                    .Add(storedMessage);
            }

            return CloneMessage(storedMessage);
        }

        public List<Message> GetGroupHistory(
            int groupId,
            int requestedBy,
            int limit = 100)
        {
            if (groupId <= 0 ||
                requestedBy <= 0)
            {
                throw new ArgumentException(
                    "Thông tin lấy lịch sử không hợp lệ.");
            }

            if (!groupManager.IsMember(
                groupId,
                requestedBy))
            {
                throw new UnauthorizedAccessException(
                    "Bạn không phải thành viên của nhóm.");
            }

            int safeLimit = limit;

            if (safeLimit <= 0)
            {
                safeLimit = 100;
            }

            if (safeLimit > 500)
            {
                safeLimit = 500;
            }

            lock (historyLock)
            {
                if (!histories.TryGetValue(
                    groupId,
                    out List<Message>? messages))
                {
                    return new List<Message>();
                }

                return messages
                    .OrderBy(message =>
                        message.Timestamp)
                    .ThenBy(message =>
                        message.Id)
                    .TakeLast(safeLimit)
                    .Select(CloneMessage)
                    .ToList();
            }
        }

        public Message HandleGetGroupHistoryRequest(
            Message requestMessage)
        {
            if (requestMessage.Type !=
                MessageType.GetChatHistoryRequest)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải yêu cầu lấy lịch sử.");
            }

            int groupId = 0;

            GroupHistoryResponse response;

            try
            {
                GroupHistoryRequest? request =
                    JsonSerializer
                        .Deserialize<GroupHistoryRequest>(
                            requestMessage.Content);

                if (request == null)
                {
                    throw new ArgumentException(
                        "Dữ liệu yêu cầu không hợp lệ.");
                }

                groupId = request.GroupId;

                List<Message> messages =
                    GetGroupHistory(
                        request.GroupId,
                        requestMessage.SenderId,
                        request.Limit);

                response =
                    new GroupHistoryResponse
                    {
                        Success = true,
                        Message =
                            "Lấy lịch sử nhóm thành công.",
                        GroupId = request.GroupId,
                        Messages = messages
                    };
            }
            catch (Exception ex)
            {
                response =
                    new GroupHistoryResponse
                    {
                        Success = false,
                        Message = ex.Message,
                        GroupId = groupId,
                        Messages =
                            new List<Message>()
                    };
            }

            return new Message
            {
                SenderId = 0,
                SenderName = "Server",

                ReceiverId =
                    requestMessage.SenderId,

                GroupId = groupId,

                Type =
                    MessageType.GetChatHistoryResponse,

                Content =
                    JsonSerializer.Serialize(response),

                Timestamp = DateTime.Now
            };
        }

        private static Message CloneMessage(
            Message source)
        {
            return new Message
            {
                Id = source.Id,
                SenderId = source.SenderId,
                SenderName = source.SenderName,
                ReceiverId = source.ReceiverId,
                GroupId = source.GroupId,
                Content = source.Content,
                Type = source.Type,
                Timestamp = source.Timestamp,

                ReplyToMessageId =
                    source.ReplyToMessageId,

                ReplyToSenderName =
                    source.ReplyToSenderName,

                ReplyToContent =
                    source.ReplyToContent
            };
        }
    }
}