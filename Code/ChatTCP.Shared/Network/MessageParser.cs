using System;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;

namespace ChatTCP.Shared.Network
{
    public static class MessageParser
    {
        public static string Serialize(Message message)
        {
            return $"{message.Id}|{message.SenderId}|{message.SenderName}|{message.ReceiverId}|{message.GroupId}|{message.Type}|{message.Content}|{message.ReplyToMessageId}|{message.ReplyToSenderName}|{message.ReplyToContent}";
        }

        public static Message Deserialize(string rawData)
        {
            string[] parts = rawData.Split('|');
            var msg = new Message
            {
                Id = int.Parse(parts[0]),
                SenderId = int.Parse(parts[1]),
                SenderName = parts[2],
                ReceiverId = string.IsNullOrEmpty(parts[3]) ? null : int.Parse(parts[3]),
                GroupId = string.IsNullOrEmpty(parts[4]) ? null : int.Parse(parts[4]),
                Type = (MessageType)Enum.Parse(typeof(MessageType), parts[5]),
                Content = parts[6]
            };

            if (parts.Length > 7 && !string.IsNullOrEmpty(parts[7]))
            {
                msg.ReplyToMessageId = int.Parse(parts[7]);
                msg.ReplyToSenderName = parts[8];
                msg.ReplyToContent = parts[9];
            }

            return msg;
        }
    }
}
