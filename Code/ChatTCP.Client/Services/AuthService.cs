using System;
using System.Text.Json;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
// Project bật UseWindowsForms + ImplicitUsings nên System.Windows.Forms được
// global-using tự động cho MỌI file trong project, kể cả file service thuần
// túy như file này (không hề có "using System.Windows.Forms;" tường minh).
// System.Windows.Forms cũng có 1 struct tên là Message (dùng trong WndProc),
// nên "Message" trở nên mơ hồ (CS0104) nếu không chỉ rõ - dòng alias dưới đây
// ép "Message" trong file này luôn hiểu là ChatTCP.Shared.Models.Message.
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

        // Thêm property này để LoginForm/RegisterForm kiểm tra được TRƯỚC khi gọi
        // RequestLogin/RequestRegister, thay vì phải đợi RegisterFailed/LoginFailed
        // báo lỗi "Chưa kết nối tới Server" rồi mới biết. Chỉ đơn giản proxy lại
        // TcpClientManager.IsConnected có sẵn.
        public bool IsConnected => tcpClientManager.IsConnected;

        // Expose lại TcpClientManager đã kết nối (dùng chung xuyên suốt vòng đời app)
        // để LoginForm truyền tiếp cho ClientForm sau khi đăng nhập thành công - tránh
        // ClientForm tự tạo 1 TcpClientManager MỚI, chưa từng Connect(), khiến
        // ChatService/GroupService bên trong luôn báo "Client chưa kết nối đến Server."
        public TcpClientManager Connection => tcpClientManager;

        // ĐÃ SỬA BUG NGHIÊM TRỌNG: tham số trước đây bị gõ nhầm tên thành
        // "TClientManager" (khác với tên field "tcpClientManager"), khiến dòng
        // "this.tcpClientManager = tcpClientManager;" bên dưới KHÔNG hề dùng tham số
        // truyền vào - nó tự gán field (đang là null) vào chính nó, tham số bị bỏ
        // quên hoàn toàn. Hậu quả: field tcpClientManager luôn null suốt vòng đời
        // AuthService, và dòng "tcpClientManager.MessageReceived += ..." bên dưới sẽ
        // ném NullReferenceException NGAY LẬP TỨC khi Program.cs gọi
        // "new AuthService(tcpClientManager)" - app crash khi khởi động, không liên
        // quan gì tới lỗi biên dịch Message ambiguous ở trên.
        //
        // Đặt lại đúng tên tham số khớp với tên field để gán đúng giá trị được truyền
        // vào (dùng "this." để phân biệt field và tham số, giữ đúng convention có sẵn).
        public AuthService(TcpClientManager tcpClientManager)
        {
            this.tcpClientManager = tcpClientManager;

            this.tcpClientManager.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        /// Kết nối tới Server.
        /// </summary>
        public bool Connect(string ip, int port)
        {
            return tcpClientManager.Connect(ip, port);
        }

        /// <summary>
        /// Đăng nhập.
        /// </summary>
        public void RequestLogin(string username, string password)
        {
            if (!tcpClientManager.IsConnected)
            {
                LoginFailed?.Invoke("Chưa kết nối tới Server.");
                return;
            }

            try
            {
                var request = new LoginRequestData
                {
                    Username = username,
                    Password = password
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

        /// <summary>
        /// Đăng ký tài khoản.
        /// </summary>
        public void RequestRegister(
            string username,
            string password,
            string displayName,
            string? avatarFileName = null)
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
                    Avatar = avatarFileName
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

        /// <summary>
        /// Xử lý Message nhận từ Server.
        /// </summary>
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

        /// <summary>
        /// Xử lý kết quả đăng nhập từ Server.
        /// </summary>
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

                LoginSucceeded?.Invoke(response.User);
            }
            catch
            {
                LoginFailed?.Invoke(
                    "Không thể đọc phản hồi đăng nhập từ Server."
                );
            }
        }

        /// <summary>
        /// Xử lý kết quả đăng ký từ Server.
        /// </summary>
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

                RegisterSucceeded?.Invoke(response.User);
            }
            catch
            {
                RegisterFailed?.Invoke(
                    "Không thể đọc phản hồi đăng ký từ Server."
                );
            }
        }

        /// <summary>
        /// Ngắt kết nối Server.
        /// </summary>
        public void Disconnect()
        {
            tcpClientManager.MessageReceived -= OnMessageReceived;
            tcpClientManager.Disconnect();
        }

        // =========================
        // REQUEST / RESPONSE MODELS
        // =========================

        private class LoginRequestData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        private class RegisterRequestData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            // Tên file avatar (vd "phuong12.png") đã được RegisterForm lưu sẵn vào
            // Resources/Avatars/ cục bộ. Đặt tên field "Avatar" TRÙNG với property
            // User.Avatar bên Server (Shared.Models.User) - MessageHandler.HandleRegister
            // hiện đang deserialize thẳng Content thành User nên không cần sửa gì bên
            // Server, field JSON "Avatar" tự động map đúng vào User.Avatar.
            public string? Avatar { get; set; }
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