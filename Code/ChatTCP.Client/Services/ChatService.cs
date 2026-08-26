using System;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Services
{
    public class ChatService : IDisposable
    {
        private readonly TcpClientManager? _tcpClient;

        public event Action<ChatMessage>? OnMessageReceived;
        public ChatMessage? ActiveReplyTarget { get; private set; }

        public ChatService()
        {
        }

        public ChatService(TcpClientManager tcpClient)
        {
            _tcpClient = tcpClient;
            if (_tcpClient != null)
            {
                _tcpClient.MessageReceived += HandleIncomingMessage;
            }
        }

        private void HandleIncomingMessage(ChatMessage message)
        {
            if (message.Type == MessageType.DirectChat)
            {
                OnMessageReceived?.Invoke(message);
            }
        }

        public void SendMessage(ChatMessage message)
        {
            AttachReplyInfo(message);
            _tcpClient?.SendMessage(message);
            ClearReplyTarget();
        }

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

        public void Dispose()
        {
            if (_tcpClient != null)
            {
                _tcpClient.MessageReceived -= HandleIncomingMessage;
            }
        }
    }
}
