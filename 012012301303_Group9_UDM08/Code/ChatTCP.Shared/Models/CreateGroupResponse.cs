namespace ChatTCP.Shared.Models
{
    public class CreateGroupResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Group? Group { get; set; }
    }
}