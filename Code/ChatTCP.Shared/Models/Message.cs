using System;
using ChatTCP.Shared.Enums;

namespace ChatTCP.Shared.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.DirectChat;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Reply feature properties
        public int? ReplyToMessageId { get; set; }
        public string? ReplyToSenderName { get; set; }
        public string? ReplyToContent { get; set; }
    }
}
