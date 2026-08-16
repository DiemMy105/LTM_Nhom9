using System;
using System.Collections.Generic;
using System.Linq;
using ChatTCP.Shared.Models;

namespace ChatTCP.Server.Services
{
    public class GroupManager
    {
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

            return group;
        }
    }
}