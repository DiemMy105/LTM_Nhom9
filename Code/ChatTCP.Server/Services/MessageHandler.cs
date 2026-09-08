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

        public event Action<User, ClientConnection>? UserLoggedIn;
        public event Action<User>? UserRegistered;
        public event Action<int, string>? UserLoggedOut;

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

                    case MessageType.AddGroupMemberRequest:
                    case MessageType.RemoveGroupMemberRequest:
                    case MessageType.DissolveGroupRequest:
                        HandleGroupManagement(msg, client);
                        break;

                    case MessageType.GetUserListRequest:
                        HandleGetUserList(msg, client);
                        break;

                    case MessageType.GetChatHistoryRequest:
                        HandleGetChatHistory(msg, client);
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

                UserLoggedIn?.Invoke(user, client);

                Console.WriteLine($"[MessageHandler] Người dùng \"{user.Username}\" (ID: {user.UserId}) đã đăng nhập.");

                // Thông báo tới các Client khác là user này vừa Online
                var statusMsg = new Message
                {
                    SenderId = user.UserId,
                    SenderName = user.Username,
                    Type = MessageType.UserStatusUpdate,
                    Content = JsonSerializer.Serialize(new User
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Avatar = user.Avatar,
                        Status = "Online"
                    }),
                    Timestamp = DateTime.Now
                };
                _clientManager.Broadcast(statusMsg, excludeUserId: user.UserId);
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

            if (isSuccess && registeredUser != null)
            {
                UserRegistered?.Invoke(registeredUser);
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
                int loggedOutUserId = client.UserId;
                string loggedOutUsername = client.Username ?? string.Empty;

                _dbService.UpdateUserStatus(client.UserId, "Offline");
                _clientManager.RemoveClient(client);

                UserLoggedOut?.Invoke(loggedOutUserId, loggedOutUsername);

                Console.WriteLine($"[MessageHandler] Người dùng ID {client.UserId} (\"{client.Username}\") đã đăng xuất.");

                // Thông báo tới các Client khác là user này vừa Offline
                var statusMsg = new Message
                {
                    SenderId = client.UserId,
                    SenderName = client.Username ?? string.Empty,
                    Type = MessageType.UserStatusUpdate,
                    Content = JsonSerializer.Serialize(new User
                    {
                        UserId = client.UserId,
                        Username = client.Username ?? string.Empty,
                        Status = "Offline"
                    }),
                    Timestamp = DateTime.Now
                };
                _clientManager.Broadcast(statusMsg);
            }
        }

        // Chuyển tiếp tin nhắn chat 1-1 tới người nhận
        private void HandleDirectChat(Message msg, ClientConnection client)
        {
            // Lưu tin nhắn vào CSDL
            _dbService.SaveMessage(msg);

            bool isDelivered = false;

            if (msg.ReceiverId.HasValue && msg.ReceiverId.Value > 0)
            {
                isDelivered = _clientManager.ForwardMessageToClient(msg.ReceiverId.Value, msg);
            }

            if (isDelivered)
            {
                Console.WriteLine($"[MessageHandler] Đã chuyển tin 1-1 từ User ID {msg.SenderId} tới User ID {msg.ReceiverId}");
            }
            else
            {
                Console.WriteLine($"[MessageHandler] Người nhận (ID: {msg.ReceiverId}) hiện không online hoặc không tồn tại.");
            }
        }

        // Gửi tin nhắn nhóm tới các thành viên đang online
        private void HandleGroupChat(Message msg, ClientConnection client)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg.SenderName) && !string.IsNullOrWhiteSpace(client.Username))
                {
                    msg.SenderName = client.Username;
                }

                // Lưu tin nhắn nhóm vào CSDL
                _dbService.SaveMessage(msg);

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

                // Gửi thông báo cập nhật nhóm tới tất cả các thành viên khác đang online
                try
                {
                    var groupResponse = JsonSerializer.Deserialize<CreateGroupResponse>(response.Content);
                    if (groupResponse != null && groupResponse.Success && groupResponse.Group != null)
                    {
                        foreach (int memberId in groupResponse.Group.MemberIds)
                        {
                            if (memberId != client.UserId && memberId > 0)
                            {
                                var memberNotice = new Message
                                {
                                    SenderId = 0,
                                    SenderName = "Server",
                                    ReceiverId = memberId,
                                    Type = MessageType.CreateGroupResponse,
                                    Content = response.Content,
                                    Timestamp = DateTime.Now
                                };
                                _clientManager.ForwardMessageToClient(memberId, memberNotice);
                            }
                        }
                    }
                }
                catch (Exception notifyEx)
                {
                    Console.WriteLine($"[MessageHandler] Lỗi gửi thông báo nhóm cho thành viên: {notifyEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi tạo nhóm: {ex.Message}");
            }
        }

        private void HandleGroupManagement(
            Message msg,
            ClientConnection client)
        {
            int groupId = 0;
            var recipients = new HashSet<int>();
            MessageType responseType =
                msg.Type == MessageType.AddGroupMemberRequest
                    ? MessageType.AddGroupMemberResponse
                    : msg.Type == MessageType.RemoveGroupMemberRequest
                        ? MessageType.RemoveGroupMemberResponse
                        : MessageType.DissolveGroupResponse;

            GroupManagementResponse response;

            try
            {
                if (client.UserId <= 0)
                {
                    throw new InvalidOperationException(
                        "Bạn chưa đăng nhập.");
                }

                GroupManagementRequest? request =
                    JsonSerializer.Deserialize<GroupManagementRequest>(
                        msg.Content);

                if (request == null)
                {
                    throw new InvalidOperationException(
                        "Dữ liệu quản lý nhóm không hợp lệ.");
                }

                groupId = request.GroupId;
                Group group;

                if (msg.Type == MessageType.AddGroupMemberRequest)
                {
                    group = _groupManager.AddMember(
                        request.GroupId,
                        client.UserId,
                        request.MemberId);

                    foreach (int id in group.MemberIds)
                    {
                        recipients.Add(id);
                    }
                }
                else if (msg.Type == MessageType.RemoveGroupMemberRequest)
                {
                    foreach (int id in
                        _groupManager.GetMemberIds(request.GroupId))
                    {
                        recipients.Add(id);
                    }

                    group = _groupManager.RemoveMember(
                        request.GroupId,
                        client.UserId,
                        request.MemberId);
                }
                else
                {
                    foreach (int id in
                        _groupManager.GetMemberIds(request.GroupId))
                    {
                        recipients.Add(id);
                    }

                    group = _groupManager.DissolveGroup(
                        request.GroupId,
                        client.UserId);
                }

                response = new GroupManagementResponse
                {
                    Success = true,
                    Message = "Cập nhật nhóm thành công.",
                    Group = group
                };
            }
            catch (Exception ex)
            {
                response = new GroupManagementResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Group = null
                };
            }

            Message responseMessage = new Message
            {
                SenderId = 0,
                SenderName = "Server",
                ReceiverId = client.UserId,
                GroupId = groupId,
                Type = responseType,
                Content = JsonSerializer.Serialize(response),
                Timestamp = DateTime.Now
            };

            if (!response.Success)
            {
                client.SendMessage(responseMessage);
                return;
            }

            foreach (int recipientId in recipients)
            {
                responseMessage.ReceiverId = recipientId;

                if (recipientId == client.UserId)
                {
                    client.SendMessage(responseMessage);
                }
                else
                {
                    _clientManager.ForwardMessageToClient(
                        recipientId,
                        responseMessage);
                }
            }

            Console.WriteLine(
                $"[MessageHandler] Đã cập nhật nhóm ID {groupId}.");
        }

        // Xử lý lấy danh sách nhóm của người dùng
        private void HandleGetGroupList(Message msg, ClientConnection client)
        {
            try
            {
                var userGroups = _dbService.GetGroupsForUser(client.UserId);
                if ((userGroups == null || userGroups.Count == 0) && _groupManager != null)
                {
                    userGroups = _groupManager.GetGroupsForUser(client.UserId);
                }

                Message response = new Message
                {
                    SenderId = 0,
                    SenderName = "Server",
                    ReceiverId = client.UserId,
                    Type = MessageType.GetGroupListResponse,
                    Content = JsonSerializer.Serialize(userGroups ?? new List<Group>()),
                    Timestamp = DateTime.Now
                };
                client.SendMessage(response);
                Console.WriteLine($"[MessageHandler] Đã gửi danh sách {userGroups?.Count ?? 0} nhóm cho User ID {client.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi lấy danh sách nhóm: {ex.Message}");
            }
        }

        // Xử lý lấy lịch sử tin nhắn (hỗ trợ cả Chat 1-1 và Chat Nhóm từ CSDL)
        private void HandleGetChatHistory(Message msg, ClientConnection client)
        {
            try
            {
                if (msg.GroupId.HasValue && msg.GroupId.Value > 0)
                {
                    var messages = _dbService.GetGroupChatHistory(msg.GroupId.Value);
                    var response = new GroupHistoryResponse
                    {
                        Success = true,
                        Message = "Lấy lịch sử nhóm thành công.",
                        GroupId = msg.GroupId.Value,
                        Messages = messages
                    };

                    var responseMsg = new Message
                    {
                        SenderId = 0,
                        SenderName = "Server",
                        ReceiverId = client.UserId,
                        GroupId = msg.GroupId.Value,
                        Type = MessageType.GetChatHistoryResponse,
                        Content = JsonSerializer.Serialize(response),
                        Timestamp = DateTime.Now
                    };
                    client.SendMessage(responseMsg);
                    Console.WriteLine($"[MessageHandler] Đã gửi lịch sử {messages.Count} tin nhắn nhóm ID {msg.GroupId.Value} cho User ID {client.UserId}");
                }
                else
                {
                    int partnerId = 0;
                    string partnerUsername = msg.Content?.Trim() ?? string.Empty;

                    if (msg.ReceiverId.HasValue && msg.ReceiverId.Value > 0)
                    {
                        partnerId = msg.ReceiverId.Value;
                        var partnerUser = _dbService.GetUserById(partnerId);
                        if (partnerUser != null)
                        {
                            partnerUsername = partnerUser.Username;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(partnerUsername))
                    {
                        var partnerUser = _dbService.GetUserByUsername(partnerUsername);
                        if (partnerUser != null)
                        {
                            partnerId = partnerUser.UserId;
                        }
                    }

                    if (partnerId > 0 && client.UserId > 0)
                    {
                        var messages = _dbService.GetDirectChatHistory(client.UserId, partnerId);
                        var responseMsg = new Message
                        {
                            SenderId = partnerId,
                            SenderName = partnerUsername,
                            ReceiverId = client.UserId,
                            Type = MessageType.GetChatHistoryResponse,
                            Content = JsonSerializer.Serialize(messages),
                            Timestamp = DateTime.Now
                        };
                        client.SendMessage(responseMsg);
                        Console.WriteLine($"[MessageHandler] Đã gửi lịch sử {messages.Count} tin nhắn 1-1 giữa User ID {client.UserId} và User ID {partnerId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi lấy lịch sử chat: {ex.Message}");
            }
        }

        // Xử lý lấy toàn bộ danh sách người dùng trong CSDL
        private void HandleGetUserList(Message msg, ClientConnection client)
        {
            try
            {
                var allUsers = _dbService.GetAllUsers();
                foreach (var u in allUsers)
                {
                    bool isOnline = _clientManager.GetClient(u.UserId) != null;
                    u.Status = isOnline ? "Online" : "Offline";
                    u.Password = string.Empty; // Không gửi mật khẩu
                }

                Message response = new Message
                {
                    SenderId = 0,
                    SenderName = "Server",
                    ReceiverId = client.UserId,
                    Type = MessageType.GetUserListResponse,
                    Content = JsonSerializer.Serialize(allUsers),
                    Timestamp = DateTime.Now
                };

                client.SendMessage(response);
                Console.WriteLine($"[MessageHandler] Đã gửi danh sách {allUsers.Count} user trong CSDL cho User ID {client.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageHandler] Lỗi lấy danh sách user: {ex.Message}");
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
