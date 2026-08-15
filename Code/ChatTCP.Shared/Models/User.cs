using System;

namespace ChatTCP.Shared.Models
{
    public class User
    {
        public int UserId { get; set; }
        public int Id { get => UserId; set => UserId = value; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FullName { get => DisplayName; set => DisplayName = value; }
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
