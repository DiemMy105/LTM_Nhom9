using System;
using System.Collections.Generic;

namespace ChatTCP.Shared.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        public int Id { get => GroupId; set => GroupId = value; }
        public string GroupName { get; set; } = string.Empty;
        public string Name { get => GroupName; set => GroupName = value; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<int> MemberIds { get; set; } = new List<int>();
    }
}
