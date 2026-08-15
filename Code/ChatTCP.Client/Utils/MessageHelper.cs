using System;
using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Utils
{
    public static class MessageHelper
    {
        public static string FormatReplyPreview(ChatMessage message)
        {
            if (string.IsNullOrEmpty(message.ReplyToSenderName))
                return string.Empty;

            string contentPreview = message.ReplyToContent ?? string.Empty;
            if (contentPreview.Length > 30)
            {
                contentPreview = contentPreview.Substring(0, 27) + "...";
            }

            return $"↩ Trả lời {message.ReplyToSenderName}: {contentPreview}";
        }
    }
}
