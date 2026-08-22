using System.Collections.Generic;

namespace ChatTCP.Shared.Models
{
	public class GroupMessageResult
	{
		public Message GroupMessage { get; set; }
			= new Message();

		public List<int> RecipientIds { get; set; }
			= new List<int>();
	}
}