using System;
using System.Text.Json;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using ChatTCP.Server.Network;

namespace ChatTCP.Server.Services
{
    public class MessageHandler
    {
        private readonly DatabaseService _dbService;
        private readonly ClientManager _clientManager;
        private readonly GroupManager _groupManager;

        public MessageHandler(DatabaseService dbService, ClientManager clientManager, GroupManager groupManager)
        {
            _dbService = dbService;
            _clientManager = clientManager;
            _groupManager = groupManager;
        }

        // Hàm xử lý chính khi nhận được chuỗi JSON từ Client
        public void HandleIncomingMessage(string jsonMessage, ClientConnection client)
        {
            try
            {
                // Giả định bạn dùng MessageParser hoặc JsonSerializer ở đây
                Message msg = JsonSerializer.Deserialize<Message>(jsonMessage);

                switch (msg.Type)
                {
                    case MessageType.LOGIN:
                        HandleLogin(msg, client);
                        break;
                    case MessageType.REGISTER:
                        HandleRegister(msg, client);
                        break;
                    case MessageType.CHAT_1_1:
                        _clientManager.ForwardMessageToClient(msg.Receiver, msg);
                        break;
                    case MessageType.CHAT_GROUP:
                        _groupManager.BroadcastToGroup(msg.GroupId, msg, client);
                        break;
                    // Xử lý các loại tin nhắn khác: REPLY, FORWARD, EMOJI...
                    default:
                        Console.WriteLine($"[MessageHandler] Unhandled message type: {msg.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Error parsing message: {ex.Message}");
            }
        }

        private void HandleLogin(Message msg, ClientConnection client)
        {
            // Logic đăng nhập sử dụng DatabaseService
            bool isSuccess = _dbService.AuthenticateUser(msg.Sender, msg.Content); // Content chứa password

            Message response = new Message
            {
                Type = MessageType.LOGIN_RESPONSE,
                Content = isSuccess ? "SUCCESS" : "FAIL"
            };

            if (isSuccess)
            {
                client.Username = msg.Sender;
                _clientManager.AddClient(client);
            }

            client.SendMessage(JsonSerializer.Serialize(response));
        }

        private void HandleRegister(Message msg, ClientConnection client)
        {
            // Gọi dbService đăng ký...
            bool isSuccess = _dbService.RegisterUser(msg.Sender, msg.Content);
            Message response = new Message
            {
                Type = MessageType.REGISTER_RESPONSE,
                Content = isSuccess ? "SUCCESS" : "FAIL"
            };
            client.SendMessage(JsonSerializer.Serialize(response));
        }
    }
}