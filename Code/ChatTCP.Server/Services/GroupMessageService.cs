using System;
using System.Collections.Generic;
using System.Linq;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Services
{
    public class GroupMessageService
    {
        private readonly GroupManager groupManager;

        public GroupMessageService(
            GroupManager groupManager)
        {
            this.groupManager = groupManager;
        }

        public GroupMessageResult PrepareGroupMessage(
            Message requestMessage)
        {
            if (requestMessage.Type !=
                MessageType.GroupChat)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải tin nhắn nhóm.");
            }

            if (!requestMessage.GroupId.HasValue ||
                requestMessage.GroupId.Value <= 0)
            {
                throw new ArgumentException(
                    "Mã nhóm không hợp lệ.");
            }

            if (requestMessage.SenderId <= 0)
            {
                throw new ArgumentException(
                    "Người gửi không hợp lệ.");
            }

            string content =
                requestMessage.Content.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "Nội dung tin nhắn không được để trống.");
            }

            if (content.Length > 4000)
            {
                throw new ArgumentException(
                    "Tin nhắn không được vượt quá 4000 ký tự.");
            }

            int groupId =
                requestMessage.GroupId.Value;

            if (!groupManager.IsMember(
                groupId,
                requestMessage.SenderId))
            {
                throw new UnauthorizedAccessException(
                    "Bạn không phải thành viên của nhóm.");
            }

            List<int> recipientIds =
                groupManager
                    .GetMemberIds(groupId)
                    .Where(userId =>
                        userId > 0 &&
                        userId != requestMessage.SenderId)
                    .Distinct()
                    .ToList();

            Message safeMessage = new Message
            {
                SenderId =
                    requestMessage.SenderId,

                SenderName =
                    requestMessage.SenderName,

                ReceiverId = null,
                GroupId = groupId,
                Content = content,
                Type = MessageType.GroupChat,
                Timestamp = DateTime.Now,

                ReplyToMessageId =
                    requestMessage.ReplyToMessageId,

                ReplyToSenderName =
                    requestMessage.ReplyToSenderName,

                ReplyToContent =
                    requestMessage.ReplyToContent
            };

            return new GroupMessageResult
            {
                GroupMessage = safeMessage,
                RecipientIds = recipientIds
            };
        }
    }
}