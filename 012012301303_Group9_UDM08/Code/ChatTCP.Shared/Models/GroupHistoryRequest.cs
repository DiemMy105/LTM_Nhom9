namespace ChatTCP.Shared.Models
{
    public class GroupHistoryRequest
    {
        public int GroupId { get; set; }

        public int Limit { get; set; } = 100;
    }
}