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
        private readonly object groupLock =
            new object();

        private readonly List<Group> groups =
            new List<Group>();

        private int nextGroupId = 1;

        public event Action<Group>? GroupCreated;
        public event Action<Group>? GroupUpdated;
        public event Action<Group>? GroupDissolved;

        public Group CreateGroup(
            string groupName,
            int createdBy,
            List<int>? memberIds)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException(
                    "Tên nhóm không được để trống.");
            }

            string normalizedName =
                groupName.Trim();

            if (normalizedName.Length > 100)
            {
                throw new ArgumentException(
                    "Tên nhóm không được vượt quá 100 ký tự.");
            }

            if (createdBy <= 0)
            {
                throw new ArgumentException(
                    "Người tạo nhóm không hợp lệ.");
            }

            List<int> validMemberIds =
                memberIds == null
                    ? new List<int>()
                    : memberIds
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

            if (!validMemberIds.Contains(createdBy))
            {
                validMemberIds.Add(createdBy);
            }

            Group storedGroup;

            lock (groupLock)
            {
                storedGroup = new Group
                {
                    GroupId = nextGroupId,
                    GroupName = normalizedName,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    MemberIds = validMemberIds
                };

                nextGroupId++;

                groups.Add(storedGroup);
            }

            Group result =
                CloneGroup(storedGroup);

            GroupCreated?.Invoke(result);

            return result;
        }

        public List<Group> GetGroups()
        {
            lock (groupLock)
            {
                return groups
                    .Select(CloneGroup)
                    .ToList();
            }
        }

        public List<Group> GetGroupsForUser(
            int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "Người dùng không hợp lệ.");
            }

            lock (groupLock)
            {
                return groups
                    .Where(group =>
                        group.MemberIds
                            .Contains(userId))
                    .Select(CloneGroup)
                    .ToList();
            }
        }

        public Group? GetGroupById(
            int groupId)
        {
            if (groupId <= 0)
            {
                return null;
            }

            lock (groupLock)
            {
                Group? group =
                    groups.FirstOrDefault(
                        item =>
                            item.GroupId == groupId);

                if (group == null)
                {
                    return null;
                }

                return CloneGroup(group);
            }
        }

        public List<int> GetMemberIds(
            int groupId)
        {
            lock (groupLock)
            {
                Group group =
                    FindGroupLocked(groupId);

                return group.MemberIds.ToList();
            }
        }

        public bool IsOwner(
            int groupId,
            int userId)
        {
            lock (groupLock)
            {
                Group? group =
                    groups.FirstOrDefault(
                        item =>
                            item.GroupId == groupId);

                return group != null &&
                       group.IsOwner(userId);
            }
        }

        public bool IsMember(
            int groupId,
            int userId)
        {
            lock (groupLock)
            {
                Group? group =
                    groups.FirstOrDefault(
                        item =>
                            item.GroupId == groupId);

                return group != null &&
                       group.HasMember(userId);
            }
        }

        public Group AddMember(
            int groupId,
            int requestedBy,
            int memberId)
        {
            ValidateManagementIds(
                groupId,
                requestedBy,
                memberId);

            Group result;

            lock (groupLock)
            {
                Group group =
                    FindGroupLocked(groupId);

                EnsureOwner(
                    group,
                    requestedBy);

                if (group.MemberIds
                    .Contains(memberId))
                {
                    throw new InvalidOperationException(
                        "Người dùng đã là thành viên của nhóm.");
                }

                group.MemberIds.Add(memberId);

                result = CloneGroup(group);
            }

            GroupUpdated?.Invoke(result);

            return result;
        }

        public Group RemoveMember(
            int groupId,
            int requestedBy,
            int memberId)
        {
            ValidateManagementIds(
                groupId,
                requestedBy,
                memberId);

            Group result;

            lock (groupLock)
            {
                Group group =
                    FindGroupLocked(groupId);

                EnsureOwner(
                    group,
                    requestedBy);

                if (group.CreatedBy == memberId)
                {
                    throw new InvalidOperationException(
                        "Không thể xóa trưởng nhóm khỏi nhóm.");
                }

                bool removed =
                    group.MemberIds.Remove(memberId);

                if (!removed)
                {
                    throw new InvalidOperationException(
                        "Người dùng không thuộc nhóm.");
                }

                result = CloneGroup(group);
            }

            GroupUpdated?.Invoke(result);

            return result;
        }

        public Group DissolveGroup(
            int groupId,
            int requestedBy)
        {
            if (groupId <= 0 ||
                requestedBy <= 0)
            {
                throw new ArgumentException(
                    "Thông tin giải tán nhóm không hợp lệ.");
            }

            Group result;

            lock (groupLock)
            {
                Group group =
                    FindGroupLocked(groupId);

                EnsureOwner(
                    group,
                    requestedBy);

                result = CloneGroup(group);

                groups.Remove(group);
            }

            GroupDissolved?.Invoke(result);

            return result;
        }

        public Message HandleCreateGroupRequest(
            Message requestMessage)
        {
            if (requestMessage.Type !=
                MessageType.CreateGroupRequest)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải yêu cầu tạo nhóm.");
            }

            CreateGroupResponse response;

            try
            {
                CreateGroupRequest? request =
                    JsonSerializer
                        .Deserialize<CreateGroupRequest>(
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

                    Message =
                        "Tạo nhóm thành công.",

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

                ReceiverId =
                    requestMessage.SenderId,

                Type =
                    MessageType.CreateGroupResponse,

                Content =
                    JsonSerializer.Serialize(response),

                Timestamp = DateTime.Now
            };
        }

        public Message HandleGetGroupListRequest(
            Message requestMessage)
        {
            if (requestMessage.Type !=
                MessageType.GetGroupListRequest)
            {
                throw new ArgumentException(
                    "Tin nhắn không phải yêu cầu lấy danh sách nhóm.");
            }

            List<Group> userGroups =
                GetGroupsForUser(
                    requestMessage.SenderId);

            return new Message
            {
                SenderId = 0,
                SenderName = "Server",

                ReceiverId =
                    requestMessage.SenderId,

                Type =
                    MessageType.GetGroupListResponse,

                Content =
                    JsonSerializer.Serialize(
                        userGroups),

                Timestamp = DateTime.Now
            };
        }

        private Group FindGroupLocked(
            int groupId)
        {
            if (groupId <= 0)
            {
                throw new ArgumentException(
                    "Mã nhóm không hợp lệ.");
            }

            Group? group =
                groups.FirstOrDefault(
                    item =>
                        item.GroupId == groupId);

            if (group == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy nhóm.");
            }

            return group;
        }

        private static void EnsureOwner(
            Group group,
            int requestedBy)
        {
            if (!group.IsOwner(requestedBy))
            {
                throw new UnauthorizedAccessException(
                    "Chỉ trưởng nhóm mới được thực hiện thao tác này.");
            }
        }

        private static void ValidateManagementIds(
            int groupId,
            int requestedBy,
            int memberId)
        {
            if (groupId <= 0 ||
                requestedBy <= 0 ||
                memberId <= 0)
            {
                throw new ArgumentException(
                    "Thông tin quản lý nhóm không hợp lệ.");
            }
        }

        private static Group CloneGroup(
            Group source)
        {
            return new Group
            {
                GroupId = source.GroupId,
                GroupName = source.GroupName,
                CreatedBy = source.CreatedBy,
                CreatedAt = source.CreatedAt,

                MemberIds =
                    source.MemberIds.ToList()
            };
        }
    }
}