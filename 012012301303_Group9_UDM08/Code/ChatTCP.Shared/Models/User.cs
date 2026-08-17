using System;

namespace ChatTCP.Shared.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = "default.png";
        public string Status { get; set; } = "Offline";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
