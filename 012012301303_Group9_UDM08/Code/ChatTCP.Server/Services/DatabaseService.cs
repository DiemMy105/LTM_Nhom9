using System.Data;
using Microsoft.Data.SqlClient;
using ChatTCP.Shared.Models;
using ChatTCP.Server.Utils;

namespace ChatTCP.Server.Services
{
    public class DatabaseService
    {
        // Kiểm tra kết nối Database
        private readonly string _connectionString;

        public DatabaseService(string? connectionString = null)
        {
            _connectionString = connectionString 
                ?? @"Server=(localdb)\MSSQLLocalDB;Database=ChatTCP;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // Đăng ký tài khoản
        public User? RegisterUser(User newUser, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (newUser == null || string.IsNullOrWhiteSpace(newUser.Username) || string.IsNullOrWhiteSpace(newUser.Password))
            {
                errorMessage = "Tên đăng nhập và mật khẩu không được để trống!";
                return null;
            }

            string username = newUser.Username.Trim();
            string displayName = string.IsNullOrWhiteSpace(newUser.DisplayName) ? username : newUser.DisplayName.Trim();
            string avatar = string.IsNullOrWhiteSpace(newUser.Avatar) ? "default.png" : newUser.Avatar.Trim();

            try
            {
                using var conn = GetConnection();
                conn.Open();

                // 1. Kiểm tra Username đã tồn tại chưa
                string checkSql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Username", username);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        errorMessage = "Tên đăng nhập đã tồn tại trong hệ thống!";
                        return null;
                    }
                }

                // 2. Băm Password
                string hashedPassword = SecurityUtils.HashPassword(newUser.Password);

                // 3. Thêm tài khoản mới vào bảng Users
                string insertSql = @"
                    INSERT INTO Users (Username, Password, DisplayName, Avatar, Status, CreatedAt)
                    VALUES (@Username, @Password, @DisplayName, @Avatar, 'Offline', GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@Username", username);
                insertCmd.Parameters.AddWithValue("@Password", hashedPassword);
                insertCmd.Parameters.AddWithValue("@DisplayName", displayName);
                insertCmd.Parameters.AddWithValue("@Avatar", avatar);

                object newIdObj = insertCmd.ExecuteScalar();
                if (newIdObj != null && int.TryParse(newIdObj.ToString(), out int newUserId))
                {
                    return new User
                    {
                        UserId = newUserId,
                        Username = username,
                        DisplayName = displayName,
                        Avatar = avatar,
                        Status = "Offline",
                        CreatedAt = DateTime.Now
                    };
                }

                errorMessage = "Lỗi hệ thống: Không thể khởi tạo UserId!";
                return null;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi CSDL: {ex.Message}";
                return null;
            }
        }

        // Đăng nhập tài khoản
        public User? LoginUser(string username, string rawPassword, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(rawPassword))
            {
                errorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return null;
            }

            try
            {
                using var conn = GetConnection();
                conn.Open();

                // 1. Truy vấn tài khoản theo Username
                string selectSql = "SELECT UserId, Username, Password, DisplayName, Avatar, Status, CreatedAt FROM Users WHERE Username = @Username";
                using var cmd = new SqlCommand(selectSql, conn);
                cmd.Parameters.AddWithValue("@Username", username.Trim());

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    errorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return null;
                }

                int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                string dbPassword = reader.GetString(reader.GetOrdinal("Password"));
                string displayName = reader.GetString(reader.GetOrdinal("DisplayName"));
                string avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? "default.png" : reader.GetString(reader.GetOrdinal("Avatar"));
                DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                reader.Close();

                // 2. So sánh mật khẩu 
                if (!SecurityUtils.VerifyPassword(rawPassword, dbPassword))
                {
                    errorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return null;
                }

                // 3. Cập nhật Status = 'Online' trong CSDL
                UpdateUserStatusInDb(conn, userId, "Online");

                // 4. Trả về thông tin User nếu đăng nhập thành công
                return new User
                {
                    UserId = userId,
                    Username = username.Trim(),
                    DisplayName = displayName,
                    Avatar = avatar,
                    Status = "Online",
                    CreatedAt = createdAt
                };
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi CSDL: {ex.Message}";
                return null;
            }
        }

        // Truy vấn thông tin User bằng ID
        public User? GetUserById(int userId)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string sql = "SELECT UserId, Username, DisplayName, Avatar, Status, CreatedAt FROM Users WHERE UserId = @UserId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
                        Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? "default.png" : reader.GetString(reader.GetOrdinal("Avatar")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Offline" : reader.GetString(reader.GetOrdinal("Status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            catch { }
            return null;
        }

        // Truy vấn thông tin User bằng Username
        public User? GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            try
            {
                using var conn = GetConnection();
                conn.Open();
                string sql = "SELECT UserId, Username, DisplayName, Avatar, Status, CreatedAt FROM Users WHERE Username = @Username";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
                        Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? "default.png" : reader.GetString(reader.GetOrdinal("Avatar")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Offline" : reader.GetString(reader.GetOrdinal("Status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            catch { }
            return null;
        }

        // Truy vấn danh sách tất cả người dùng
        public List<User> GetAllUsers()
        {
            var list = new List<User>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string sql = "SELECT UserId, Username, DisplayName, Avatar, Status, CreatedAt FROM Users ORDER BY DisplayName ASC";
                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
                        Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? "default.png" : reader.GetString(reader.GetOrdinal("Avatar")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Offline" : reader.GetString(reader.GetOrdinal("Status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
            }
            catch { }
            return list;
        }

        // Cập nhật trạng thái khi kết nối/ ngắt kết nối
        public bool UpdateUserStatus(int userId, string status)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return UpdateUserStatusInDb(conn, userId, status);
            }
            catch
            {
                return false;
            }
        }

        private bool UpdateUserStatusInDb(SqlConnection conn, int userId, string status)
        {
            string sql = "UPDATE Users SET Status = @Status WHERE UserId = @UserId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@UserId", userId);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
