using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ChatTCP.Shared.Models;

namespace ChatTCP.Client.Services
{
    /// <summary>
    /// [TV1] AuthService - PHIÊN BẢN DEMO tạm thời.
    /// Mô phỏng luồng đăng nhập / đăng ký với dữ liệu lưu trong bộ nhớ (không qua
    /// Server thật), để LoginForm/RegisterForm build và chạy thử được ngay cả khi
    /// TcpClientManager (kết nối Socket tới Server) chưa sẵn sàng.
    ///
    /// Khi TV1 hoàn thành TcpClientManager: thay toàn bộ phần demo bên dưới bằng
    /// việc gửi/nhận gói tin thật qua mạng, nhưng GIỮ NGUYÊN tên các
    /// event/method public để không phải sửa LoginForm.cs / RegisterForm.cs.
    ///
    /// Contract mà LoginForm/RegisterForm đang dùng:
    /// event Action&lt;User&gt; LoginSucceeded;      event Action&lt;string&gt; LoginFailed;
    /// event Action&lt;User&gt; RegisterSucceeded;   event Action&lt;string&gt; RegisterFailed;
    /// void RequestLogin(string username, string password);
    /// void RequestRegister(string username, string password, string displayName);
    /// </summary>
    public class AuthService
    {
        public event Action<User>? LoginSucceeded;
        public event Action<string>? LoginFailed;
        public event Action<User>? RegisterSucceeded;
        public event Action<string>? RegisterFailed;

        // Danh sách User demo dùng chung cho cả phiên chạy ứng dụng (mô phỏng Database).
        // static để mọi instance AuthService đều thấy cùng một "database" demo.
        private static readonly ConcurrentDictionary<string, User> _demoUsers =
            new ConcurrentDictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        private static int _nextUserId = 1;

        static AuthService()
        {
            // Tài khoản demo có sẵn để đăng nhập thử ngay mà không cần đăng ký trước.
            SeedDemoUser("admin", "123456", "Quản trị viên");
            SeedDemoUser("test", "123456", "Người dùng Test");
        }

        private static void SeedDemoUser(string username, string password, string displayName)
        {
            _demoUsers[username] = new User
            {
                UserId = _nextUserId++,
                Username = username,
                Password = password,
                DisplayName = displayName,
                Status = "Offline"
            };
        }

        /// <summary>
        /// Gửi yêu cầu đăng nhập. Kết quả trả về bất đồng bộ qua event
        /// LoginSucceeded/LoginFailed (mô phỏng độ trễ mạng ~500ms).
        /// </summary>
        public void RequestLogin(string username, string password)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // TODO: [TV1] Xóa dòng này khi có độ trễ mạng thật

                if (_demoUsers.TryGetValue(username, out User? user) && user.Password == password)
                {
                    user.Status = "Online";
                    LoginSucceeded?.Invoke(user);
                }
                else
                {
                    LoginFailed?.Invoke("Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            });
        }

        /// <summary>
        /// Gửi yêu cầu đăng ký tài khoản mới. Kết quả trả về bất đồng bộ qua event
        /// RegisterSucceeded/RegisterFailed.
        /// </summary>
        public void RequestRegister(string username, string password, string displayName)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // TODO: [TV1] Xóa dòng này khi có độ trễ mạng thật

                if (_demoUsers.ContainsKey(username))
                {
                    RegisterFailed?.Invoke("Tên đăng nhập đã tồn tại.");
                    return;
                }

                User newUser = new User
                {
                    UserId = _nextUserId++,
                    Username = username,
                    Password = password,
                    DisplayName = displayName,
                    Status = "Offline"
                };

                _demoUsers[username] = newUser;
                RegisterSucceeded?.Invoke(newUser);
            });
        }
    }
}