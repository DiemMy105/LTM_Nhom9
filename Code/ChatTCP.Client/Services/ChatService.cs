using System;
using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Services
{
    public class ChatService
    {
        public ChatMessage? ActiveReplyTarget { get; private set; }

        public void SetReplyTarget(ChatMessage message)
        {
            ActiveReplyTarget = message;
        }

        public void ClearReplyTarget()
        {
            ActiveReplyTarget = null;
        }

        public void AttachReplyInfo(ChatMessage messageToSend)
        {
            if (ActiveReplyTarget != null)
            {
                messageToSend.ReplyToMessageId = ActiveReplyTarget.Id;
                messageToSend.ReplyToSenderName = ActiveReplyTarget.SenderName;
                messageToSend.ReplyToContent = ActiveReplyTarget.Content;
            }
        }
    }
}
