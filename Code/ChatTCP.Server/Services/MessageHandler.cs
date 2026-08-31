using System;
using System.Collections.Generic;
using System.Text.Json;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Network;
using ChatTCP.Server.Network;
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Services
{
    // Xử lý và điều phối các tin nhắn từ Client gửi lên Server
    public class MessageHandler
    {
        private readonly DatabaseService _dbService;
        private readonly ClientManager _clientManager;
        private readonly GroupManager _groupManager;
        private readonly GroupMessageService _groupMessageService;

        public MessageHandler(DatabaseService dbService, ClientManager clientManager, GroupManager groupManager)
        {
            _dbService = dbService;
            _clientManager = clientManager;
            _groupManager = groupManager;
            _groupMessageService = new GroupMessageService(groupManager);
        }

        // Nhận chuỗi JSON từ Client, giải mã rồi chuyển tiếp xử lý
        public void HandleIncomingMessage(string jsonMessage, ClientConnection client)
        {
            try
            {
                Message? msg = MessageParser.Deserialize(jsonMessage);
                if (msg != null)
                {
                    HandleIncomingMessage(msg, client);
                }
                else
                {
                    Console.WriteLine("[MessageHandler] Gói tin rỗng hoặc sai định dạng.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi giải mã gói tin: {ex.Message}");
            }
        }

        // Phân loại MessageType để gọi hàm xử lý tương ứng
        public void HandleIncomingMessage(Message msg, ClientConnection client)
        {
            try
            {
                switch (msg.Type)
                {
                    case MessageType.LoginRequest:
                        HandleLogin(msg, client);
                        break;

                    case MessageType.RegisterRequest:
                        HandleRegister(msg, client);
                        break;

                    case MessageType.LogoutRequest:
                        HandleLogout(msg, client);
                        break;

                    case MessageType.DirectChat:
                        HandleDirectChat(msg, client);
                        break;

                    case MessageType.GroupChat:
                        HandleGroupChat(msg, client);
                        break;

                    case MessageType.CreateGroupRequest:
                        HandleCreateGroup(msg, client);
                        break;

                    case MessageType.GetGroupListRequest:
                        HandleGetGroupList(msg, client);
                        break;

                    default:
                        Console.WriteLine($"[MessageHandler] Chưa hỗ trợ loại tin nhắn: {msg.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi xử lý tin nhắn {msg.Type}: {ex.Message}");
            }
        }

        private void HandleLogin(Message msg, ClientConnection client)
        {
            string username = msg.SenderName;
            string password = msg.Content;

            if (!string.IsNullOrWhiteSpace(msg.Content) && msg.Content.TrimStart().StartsWith("{"))
            {
                try
                {
                    var loginData = JsonSerializer.Deserialize<LoginRequestData>(msg.Content);
                    if (loginData != null)
                    {
                        if (!string.IsNullOrWhiteSpace(loginData.Username))
                        {
                            username = loginData.Username;
                        }
                        password = loginData.Password;
                    }
                }
                catch
                {
                 
                }
            }

            // Kiểm tra thông tin với CSDL
            User? user = _dbService.LoginUser(username, password, out string errorMessage);
            bool isSuccess = (user != null);
            var responseData = new LoginResponseData
            {
                Success = isSuccess,
                Message = isSuccess ? "Đăng nhập thành công" : (string.IsNullOrEmpty(errorMessage) ? "Đăng nhập thất bại" : errorMessage),
                User = isSuccess ? user : null
            };

            Message response = new Message
            {
                SenderId = 0,
                SenderName = "Server",
                ReceiverId = isSuccess ? user!.UserId : null,
                Type = MessageType.LoginResponse,
                Content = JsonSerializer.Serialize(responseData),
                Timestamp = DateTime.Now
            };

            // Nếu đúng tài khoản: lưu UserId, Username vào ClientConnection và thêm vào danh sách Online
            if (isSuccess && user != null)
            {
                client.UserId = user.UserId;
                client.Username = user.Username;
                _clientManager.AddClient(client);

                Console.WriteLine($"[MessageHandler] Người dùng \"{user.Username}\" (ID: {user.UserId}) đã đăng nhập.");
            }
            else
            {
                Console.WriteLine($"[MessageHandler] Đăng nhập thất bại cho \"{username}\": {errorMessage}");
            }

            client.SendMessage(response);
        }

        // Xử lý đăng ký tài khoản mới
        private void HandleRegister(Message msg, ClientConnection client)
        {
            User newUser;

            // Đọc thông tin User nếu gửi dạng JSON
            if (!string.IsNullOrWhiteSpace(msg.Content) && msg.Content.TrimStart().StartsWith("{"))
            {
                try
                {
                    newUser = JsonSerializer.Deserialize<User>(msg.Content) ?? new User();
                }
                catch
                {
                    newUser = new User();
                }
            }
            else
            {
                newUser = new User
                {
                    Username = msg.SenderName,
                    Password = msg.Content,
                    DisplayName = msg.SenderName
                };
            }

            if (string.IsNullOrWhiteSpace(newUser.Username))
            {
                newUser.Username = msg.SenderName;
            }

            // Lưu người dùng mới vào CSDL
            User? registeredUser = _dbService.RegisterUser(newUser, out string errorMessage);
            bool isSuccess = (registeredUser != null);

            var responseData = new RegisterResponseData
            {
                Success = isSuccess,
                Message = isSuccess ? "Đăng ký tài khoản thành công" : (string.IsNullOrEmpty(errorMessage) ? "Đăng ký thất bại" : errorMessage),
                User = isSuccess ? registeredUser : null
            };

            Message response = new Message
            {
                SenderId = 0,
                SenderName = "Server",
                ReceiverId = isSuccess ? registeredUser!.UserId : null,
                Type = MessageType.RegisterResponse,
                Content = JsonSerializer.Serialize(responseData),
                Timestamp = DateTime.Now
            };

            if (isSuccess)
            {
                Console.WriteLine($"[MessageHandler] Đăng ký thành công: \"{newUser.Username}\"");
            }
            else
            {
                Console.WriteLine($"[MessageHandler] Đăng ký thất bại cho \"{newUser.Username}\": {errorMessage}");
            }

            client.SendMessage(response);
        }

        // Xử lý đăng xuất tài khoản
        private void HandleLogout(Message msg, ClientConnection client)
        {
            if (client.UserId > 0)
            {
                _dbService.UpdateUserStatus(client.UserId, "Offline");
                _clientManager.RemoveClient(client);
                Console.WriteLine($"[MessageHandler] Người dùng ID {client.UserId} (\"{client.Username}\") đã đăng xuất.");
            }
        }

        // Chuyển tiếp tin nhắn chat 1-1 tới người nhận
        private void HandleDirectChat(Message msg, ClientConnection client)
        {
            bool isDelivered = false;

            if (msg.ReceiverId.HasValue && msg.ReceiverId.Value > 0)
            {
                isDelivered = _clientManager.ForwardMessageToClient(msg.ReceiverId.Value, msg);
            }
            else if (!string.IsNullOrWhiteSpace(msg.SenderName))
            {
                isDelivered = _clientManager.ForwardMessageToClient(msg.SenderName, msg);
            }

            if (isDelivered)
            {
                Console.WriteLine($"[MessageHandler] Đã chuyển tin 1-1 từ User ID {msg.SenderId} tới User ID {msg.ReceiverId}");
            }
            else
            {
                Console.WriteLine($"[MessageHandler] Người nhận (ID: {msg.ReceiverId}) hiện không online.");
            }
        }

        // Gửi tin nhắn nhóm tới các thành viên đang online
        private void HandleGroupChat(Message msg, ClientConnection client)
        {
            try
            {
                var result = _groupMessageService.PrepareGroupMessage(msg);
                int sentCount = 0;

                foreach (int recipientId in result.RecipientIds)
                {
                    if (_clientManager.ForwardMessageToClient(recipientId, result.GroupMessage))
                    {
                        sentCount++;
                    }
                }

                Console.WriteLine($"[MessageHandler] Gửi tin nhóm (ID: {msg.GroupId}) tới {sentCount}/{result.RecipientIds.Count} thành viên online.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi tin nhắn nhóm ID {msg.GroupId}: {ex.Message}");
            }
        }

        // Xử lý tạo nhóm chat mới
        private void HandleCreateGroup(Message msg, ClientConnection client)
        {
            try
            {
                Message response = _groupManager.HandleCreateGroupRequest(msg);
                client.SendMessage(response);
                Console.WriteLine($"[MessageHandler] Đã xử lý tạo nhóm từ User ID {msg.SenderId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi tạo nhóm: {ex.Message}");
            }
        }

        // Xử lý lấy danh sách nhóm của người dùng
        private void HandleGetGroupList(Message msg, ClientConnection client)
        {
            try
            {
                Message response = _groupManager.HandleGetGroupListRequest(msg);
                client.SendMessage(response);
                Console.WriteLine($"[MessageHandler] Đã gửi danh sách nhóm cho User ID {msg.SenderId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi lấy danh sách nhóm: {ex.Message}");
            }
        }

        private class LoginRequestData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        private class LoginResponseData
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public User? User { get; set; }
        }

        private class RegisterResponseData
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public User? User { get; set; }
        }
    }
}