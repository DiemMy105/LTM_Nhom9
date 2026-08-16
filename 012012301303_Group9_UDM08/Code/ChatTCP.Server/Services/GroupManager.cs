using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Services
{
    public class GroupManager
    {
        private readonly object groupLock = new object();
        private readonly List<Group> groups = new List<Group>();
        private int nextGroupId = 1;

        public event Action<Group>? GroupCreated;

        public Group CreateGroup(
            string groupName,
            int createdBy,
            List<int> memberIds)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException(
                    "Tên nhóm không được để trống.");
            }

            if (groupName.Trim().Length > 100)
            {
                throw new ArgumentException(
                    "Tên nhóm không được vượt quá 100 ký tự.");
            }

            if (groupName.Contains('|'))
            {
                throw new ArgumentException(
                    "Tên nhóm không được chứa ký tự |.");
            }

            if (createdBy <= 0)
            {
                throw new ArgumentException(
                    "Người tạo nhóm không hợp lệ.");
            }

            List<int> validMemberIds = memberIds == null
                ? new List<int>()
                : memberIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

            if (!validMemberIds.Contains(createdBy))
            {
                validMemberIds.Add(createdBy);
            }

            Group group = new Group
            {
                GroupName = groupName.Trim(),
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now,
                MemberIds = validMemberIds
            };

            lock (groupLock)
            {
                group.GroupId = nextGroupId;
                nextGroupId++;

                groups.Add(group);
            }

            GroupCreated?.Invoke(group);

            return group;
        }

        public List<Group> GetGroups()
        {
            lock (groupLock)
            {
                return groups.ToList();
            }
        }

        public Message HandleCreateGroupRequest(Message requestMessage)
        {
            if (requestMessage.Type != MessageType.CreateGroupRequest)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải yêu cầu tạo nhóm.");
            }

            CreateGroupResponse response;

            try
            {
                CreateGroupRequest? request =
                    JsonSerializer.Deserialize<CreateGroupRequest>(
                        requestMessage.Content);

                if (request == null)
                {
                    throw new ArgumentException(
                        "Dữ liệu tạo nhóm không hợp lệ.");
                }

                Group group = CreateGroup(
                    request.GroupName,
                    requestMessage.SenderId,
                    request.MemberIds);

                response = new CreateGroupResponse
                {
                    Success = true,
                    Message = "Tạo nhóm thành công.",
                    Group = group
                };
            }
            catch (Exception ex)
            {
                response = new CreateGroupResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Group = null
                };
            }

            return new Message
            {
                SenderId = 0,
                SenderName = "Server",
                ReceiverId = requestMessage.SenderId,
                Type = MessageType.CreateGroupResponse,
                Content = JsonSerializer.Serialize(response),
                Timestamp = DateTime.Now
            };
        }
    }
}