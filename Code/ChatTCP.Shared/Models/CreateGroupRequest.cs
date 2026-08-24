using System.Collections.Generic;

namespace ChatTCP.Shared.Models
{
	public class CreateGroupRequest
	{
		public string GroupName { get; set; } = string.Empty;
		public List<int> MemberIds { get; set; } = new List<int>();
	}
}