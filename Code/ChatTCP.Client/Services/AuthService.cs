using System;
using System.Text.Json;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;


using Message = ChatTCP.Shared.Models.Message;


namespace ChatTCP.Client.Services
{
    public class AuthService
    {
        private readonly TcpClientManager tcpClientManager;
        public event Action<User>? LoginSucceeded;
        public event Action<string>? LoginFailed;
        public event Action<User>? RegisterSucceeded;
        public event Action<string>? RegisterFailed;
        public bool IsConnected => tcpClientManager.IsConnected;
        public TcpClientManager Connection => tcpClientManager;
        public AuthService(TcpClientManager tcpClientManager)
        {
            this.tcpClientManager = tcpClientManager;
            this.tcpClientManager.MessageReceived += OnMessageReceived;
        }
        /// Kết nối tới Server.
        public bool Connect(string ip, int port)
        {
            return tcpClientManager.Connect(ip, port);
        }
        /// Đăng nhập.

        public void RequestLogin(string username, string password)
        {
            if (!tcpClientManager.IsConnected)
            {
                LoginFailed?.Invoke("Chưa kết nối tới Server.");
                return;
            }
            try
            {
                string? avatarData = Utils.ImageUtils.GetAvatarBase64($"{username}.png");
                var request = new LoginRequestData
                {
                    Username = username,
                    Password = password,
                    AvatarData = avatarData
                };
                string json = JsonSerializer.Serialize(request);
                Message message = new Message
                {
                    Type = MessageType.LoginRequest,
                    SenderId = 0,
                    SenderName = username,
                    Content = json,
                    Timestamp = DateTime.Now
                };
                tcpClientManager.SendMessage(message);
            }
            catch (Exception ex)
            {
                LoginFailed?.Invoke(
                    "Không thể gửi yêu cầu đăng nhập: " + ex.Message
                );
            }
        }
        /// Đăng ký tài khoản.
        public void RequestRegister(
            string username,
            string password,
            string displayName,
            string? avatarFileName = null,
            string? avatarData = null)
        {
            if (!tcpClientManager.IsConnected)
            {
                RegisterFailed?.Invoke("Chưa kết nối tới Server.");
                return;
            }
            try
            {
                var request = new RegisterRequestData
                {
                    Username = username,
                    Password = password,
                    DisplayName = displayName,
                    Avatar = avatarFileName,
                    AvatarData = avatarData
                };
                string json = JsonSerializer.Serialize(request);
                Message message = new Message
                {
                    Type = MessageType.RegisterRequest,
                    SenderId = 0,
                    SenderName = username,
                    Content = json,
                    Timestamp = DateTime.Now
                };
                tcpClientManager.SendMessage(message);
            }
            catch (Exception ex)
            {
                RegisterFailed?.Invoke(
                    "Không thể gửi yêu cầu đăng ký: " + ex.Message
                );
            }
        }
        /// Xử lý Message nhận
        private void OnMessageReceived(Message message)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageType.LoginResponse:
                        HandleLoginResponse(message);
                        break;




                    case MessageType.RegisterResponse:
                        HandleRegisterResponse(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                switch (message.Type)
                {
                    case MessageType.LoginResponse:
                        LoginFailed?.Invoke(
                            "Lỗi xử lý phản hồi đăng nhập: " + ex.Message
                        );
                        break;




                    case MessageType.RegisterResponse:
                        RegisterFailed?.Invoke(
                            "Lỗi xử lý phản hồi đăng ký: " + ex.Message
                        );
                        break;
                }
            }
        }
        /// Xử lý kết quả đăng nhập từ Server.

        private void HandleLoginResponse(Message message)
        {
            try
            {
                LoginResponseData? response =
                    JsonSerializer.Deserialize<LoginResponseData>(
                        message.Content
                    );




                if (response == null)
                {
                    LoginFailed?.Invoke(
                        "Server trả về dữ liệu đăng nhập không hợp lệ."
                    );
                    return;
                }




                if (!response.Success)
                {
                    LoginFailed?.Invoke(
                        string.IsNullOrWhiteSpace(response.Message)
                            ? "Đăng nhập thất bại."
                            : response.Message
                    );




                    return;
                }




                if (response.User == null)
                {
                    LoginFailed?.Invoke(
                        "Đăng nhập thành công nhưng Server không trả về thông tin người dùng."
                    );




                    return;
                }


                if (!string.IsNullOrWhiteSpace(response.User.AvatarData))
                {
                    Utils.ImageUtils.SaveAvatarFromBase64(response.User.Avatar, response.User.AvatarData);
                }


                LoginSucceeded?.Invoke(response.User);
            }
            catch
            {
                LoginFailed?.Invoke(
                    "Không thể đọc phản hồi đăng nhập từ Server."
                );
            }
        }

        /// Xử lý kết quả đăng ký từ Server.

        private void HandleRegisterResponse(Message message)
        {
            try
            {
                RegisterResponseData? response =
                    JsonSerializer.Deserialize<RegisterResponseData>(
                        message.Content
                    );




                if (response == null)
                {
                    RegisterFailed?.Invoke(
                        "Server trả về dữ liệu đăng ký không hợp lệ."
                    );
                    return;
                }
                if (!response.Success)
                {
                    RegisterFailed?.Invoke(
                        string.IsNullOrWhiteSpace(response.Message)
                            ? "Đăng ký thất bại."
                            : response.Message
                    );
                    return;
                }
                if (response.User == null)
                {
                    RegisterFailed?.Invoke(
                        "Đăng ký thành công nhưng Server không trả về thông tin người dùng."
                    );
                    return;
                }


                if (!string.IsNullOrWhiteSpace(response.User.AvatarData))
                {
                    Utils.ImageUtils.SaveAvatarFromBase64(response.User.Avatar, response.User.AvatarData);
                }


                RegisterSucceeded?.Invoke(response.User);
            }
            catch
            {
                RegisterFailed?.Invoke(
                    "Không thể đọc phản hồi đăng ký từ Server."
                );
            }
        }
        /// Ngắt kết nối Server.
        public void Disconnect()
        {
            tcpClientManager.MessageReceived -= OnMessageReceived;
            tcpClientManager.Disconnect();
        }
        // REQUEST / RESPONSE MODELS
        private class LoginRequestData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? AvatarData { get; set; }
        }
        private class RegisterRequestData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? Avatar { get; set; }
            public string? AvatarData { get; set; }
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