using System.Collections.Generic;

namespace ChatTCP.Shared.Models
{
    public class GroupHistoryResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public int GroupId { get; set; }

        public List<Message> Messages { get; set; }
            = new List<Message>();
    }
}