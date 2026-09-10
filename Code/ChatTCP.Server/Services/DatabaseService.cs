using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using ChatTCP.Server.Utils;
using Message = ChatTCP.Shared.Models.Message;

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
                string sql = "SELECT UserId, Username, DisplayName, Avatar, Status, CreatedAt FROM Users ORDER BY UserId ASC";
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

        // Xóa hoàn toàn người dùng và toàn bộ dữ liệu liên quan khỏi CSDL
        public bool DeleteUser(int userId, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (userId <= 0)
            {
                errorMessage = "UserId không hợp lệ!";
                return false;
            }

            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var tran = conn.BeginTransaction();

                try
                {
                    // 1. Gỡ bỏ tham chiếu ReplyToMessageId trỏ tới các tin nhắn của user sắp bị xóa
                    string unlinkReplySql = @"
                        UPDATE Messages 
                        SET ReplyToMessageId = NULL 
                        WHERE ReplyToMessageId IN (
                            SELECT MessageId FROM Messages WHERE SenderId = @UserId OR ReceiverId = @UserId
                        )";
                    using (var cmd = new SqlCommand(unlinkReplySql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Xóa toàn bộ tin nhắn liên quan (gửi hoặc nhận bởi user này)
                    string deleteMessagesSql = @"
                        DELETE FROM Messages 
                        WHERE SenderId = @UserId OR ReceiverId = @UserId";
                    using (var cmd = new SqlCommand(deleteMessagesSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Xóa user khỏi bảng GroupMembers
                    string deleteMembersSql = @"
                        DELETE FROM GroupMembers 
                        WHERE UserId = @UserId";
                    using (var cmd = new SqlCommand(deleteMembersSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Cập nhật các nhóm do user này tạo (CreatedBy = NULL)
                    string updateGroupsSql = @"
                        UPDATE Groups 
                        SET CreatedBy = NULL 
                        WHERE CreatedBy = @UserId";
                    using (var cmd = new SqlCommand(updateGroupsSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // 5. Xóa user khỏi bảng Users
                    string deleteUserSql = @"
                        DELETE FROM Users 
                        WHERE UserId = @UserId";
                    using (var cmd = new SqlCommand(deleteUserSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0)
                        {
                            tran.Rollback();
                            errorMessage = "Không tìm thấy người dùng cần xóa trong CSDL.";
                            return false;
                        }
                    }

                    tran.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    errorMessage = ex.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }


        // QUẢN LÝ TIN NHẮN (MESSAGES)

        // Lưu tin nhắn vào CSDL (hỗ trợ cả Chat 1-1 và Nhóm, Reply, Forward)
        public bool SaveMessage(Message msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Content) || msg.SenderId <= 0)
                return false;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = @"
                    INSERT INTO Messages (SenderId, ReceiverId, GroupId, Content, MessageType, ReplyToMessageId, IsForwarded, SentAt)
                    VALUES (@SenderId, @ReceiverId, @GroupId, @Content, @MessageType, @ReplyToMessageId, @IsForwarded, @SentAt);
                    SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SenderId", msg.SenderId);
                cmd.Parameters.AddWithValue("@ReceiverId", (object?)msg.ReceiverId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GroupId", (object?)msg.GroupId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Content", msg.Content.Trim());
                cmd.Parameters.AddWithValue("@MessageType", msg.Type.ToString());
                cmd.Parameters.AddWithValue("@ReplyToMessageId", (object?)msg.ReplyToMessageId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsForwarded", msg.IsForward);
                cmd.Parameters.AddWithValue("@SentAt", msg.Timestamp > DateTime.MinValue ? msg.Timestamp : DateTime.Now);

                object newId = cmd.ExecuteScalar();
                if (newId != null && int.TryParse(newId.ToString(), out int messageId))
                {
                    msg.Id = messageId;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi lưu tin nhắn: {ex.Message}");
            }
            return false;
        }

        // Lấy lịch sử chat 1-1 giữa 2 người dùng
        public List<Message> GetDirectChatHistory(int user1Id, int user2Id, int limit = 100)
        {
            var list = new List<Message>();
            if (user1Id <= 0 || user2Id <= 0) return list;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = $@"
                    SELECT TOP (@Limit)
                        m.MessageId, m.SenderId, u.Username AS SenderName, m.ReceiverId, m.GroupId,
                        m.Content, m.ReplyToMessageId, m.IsForwarded, m.SentAt,
                        rm.Content AS ReplyContent, ru.Username AS ReplySenderName
                    FROM Messages m
                    INNER JOIN Users u ON m.SenderId = u.UserId
                    LEFT JOIN Messages rm ON m.ReplyToMessageId = rm.MessageId
                    LEFT JOIN Users ru ON rm.SenderId = ru.UserId
                    WHERE m.GroupId IS NULL 
                      AND ((m.SenderId = @User1 AND m.ReceiverId = @User2) OR (m.SenderId = @User2 AND m.ReceiverId = @User1))
                    ORDER BY m.SentAt ASC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Limit", limit > 0 ? limit : 100);
                cmd.Parameters.AddWithValue("@User1", user1Id);
                cmd.Parameters.AddWithValue("@User2", user2Id);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Message
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("MessageId")),
                        SenderId = reader.GetInt32(reader.GetOrdinal("SenderId")),
                        SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                        ReceiverId = reader.IsDBNull(reader.GetOrdinal("ReceiverId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                        GroupId = null,
                        Content = reader.GetString(reader.GetOrdinal("Content")),
                        Type = MessageType.DirectChat,
                        ReplyToMessageId = reader.IsDBNull(reader.GetOrdinal("ReplyToMessageId")) ? null : reader.GetInt32(reader.GetOrdinal("ReplyToMessageId")),
                        ReplyToSenderName = reader.IsDBNull(reader.GetOrdinal("ReplySenderName")) ? null : reader.GetString(reader.GetOrdinal("ReplySenderName")),
                        ReplyToContent = reader.IsDBNull(reader.GetOrdinal("ReplyContent")) ? null : reader.GetString(reader.GetOrdinal("ReplyContent")),
                        IsForward = !reader.IsDBNull(reader.GetOrdinal("IsForwarded")) && reader.GetBoolean(reader.GetOrdinal("IsForwarded")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("SentAt"))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi lấy lịch sử chat 1-1: {ex.Message}");
            }
            return list;
        }

        // Lấy lịch sử chat nhóm theo GroupId
        public List<Message> GetGroupChatHistory(int groupId, int limit = 100)
        {
            var list = new List<Message>();
            if (groupId <= 0) return list;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = $@"
                    SELECT TOP (@Limit)
                        m.MessageId, m.SenderId, u.Username AS SenderName, m.ReceiverId, m.GroupId,
                        m.Content, m.ReplyToMessageId, m.IsForwarded, m.SentAt,
                        rm.Content AS ReplyContent, ru.Username AS ReplySenderName
                    FROM Messages m
                    INNER JOIN Users u ON m.SenderId = u.UserId
                    LEFT JOIN Messages rm ON m.ReplyToMessageId = rm.MessageId
                    LEFT JOIN Users ru ON rm.SenderId = ru.UserId
                    WHERE m.GroupId = @GroupId
                    ORDER BY m.SentAt ASC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Limit", limit > 0 ? limit : 100);
                cmd.Parameters.AddWithValue("@GroupId", groupId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Message
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("MessageId")),
                        SenderId = reader.GetInt32(reader.GetOrdinal("SenderId")),
                        SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                        ReceiverId = null,
                        GroupId = groupId,
                        Content = reader.GetString(reader.GetOrdinal("Content")),
                        Type = MessageType.GroupChat,
                        ReplyToMessageId = reader.IsDBNull(reader.GetOrdinal("ReplyToMessageId")) ? null : reader.GetInt32(reader.GetOrdinal("ReplyToMessageId")),
                        ReplyToSenderName = reader.IsDBNull(reader.GetOrdinal("ReplySenderName")) ? null : reader.GetString(reader.GetOrdinal("ReplySenderName")),
                        ReplyToContent = reader.IsDBNull(reader.GetOrdinal("ReplyContent")) ? null : reader.GetString(reader.GetOrdinal("ReplyContent")),
                        IsForward = !reader.IsDBNull(reader.GetOrdinal("IsForwarded")) && reader.GetBoolean(reader.GetOrdinal("IsForwarded")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("SentAt"))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi lấy lịch sử chat nhóm: {ex.Message}");
            }
            return list;
        }

        // QUẢN LÝ NHÓM (GROUPS & GROUP MEMBERS)

        // Tạo nhóm mới và lưu vào CSDL
        public Group? CreateGroup(string groupName, int createdBy, List<int>? memberIds)
        {
            if (string.IsNullOrWhiteSpace(groupName) || createdBy <= 0) return null;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string insertGroupSql = @"
                    INSERT INTO Groups (GroupName, CreatedBy, CreatedAt)
                    VALUES (@GroupName, @CreatedBy, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(insertGroupSql, conn);
                cmd.Parameters.AddWithValue("@GroupName", groupName.Trim());
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy);

                object newId = cmd.ExecuteScalar();
                if (newId != null && int.TryParse(newId.ToString(), out int groupId))
                {
                    var allMemberIds = (memberIds ?? new List<int>()).Distinct().ToList();
                    if (!allMemberIds.Contains(createdBy))
                    {
                        allMemberIds.Add(createdBy);
                    }

                    foreach (int mId in allMemberIds)
                    {
                        if (mId <= 0) continue;
                        try
                        {
                            string insertMemberSql = "INSERT INTO GroupMembers (GroupId, UserId, JoinedAt) VALUES (@GroupId, @UserId, GETDATE())";
                            using var mCmd = new SqlCommand(insertMemberSql, conn);
                            mCmd.Parameters.AddWithValue("@GroupId", groupId);
                            mCmd.Parameters.AddWithValue("@UserId", mId);
                            mCmd.ExecuteNonQuery();
                        }
                        catch { }
                    }

                    return new Group
                    {
                        GroupId = groupId,
                        GroupName = groupName.Trim(),
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now,
                        MemberIds = allMemberIds
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi tạo nhóm trong CSDL: {ex.Message}");
            }
            return null;
        }

        // Lấy danh sách các nhóm mà User tham gia
        public List<Group> GetGroupsForUser(int userId)
        {
            var list = new List<Group>();
            if (userId <= 0) return list;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = @"
                    SELECT g.GroupId, g.GroupName, g.CreatedBy, g.CreatedAt
                    FROM Groups g
                    INNER JOIN GroupMembers gm ON g.GroupId = gm.GroupId
                    WHERE gm.UserId = @UserId
                    ORDER BY g.GroupName ASC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = cmd.ExecuteReader();
                var groupRows = new List<(int GroupId, string GroupName, int CreatedBy, DateTime CreatedAt)>();
                while (reader.Read())
                {
                    groupRows.Add((
                        reader.GetInt32(reader.GetOrdinal("GroupId")),
                        reader.GetString(reader.GetOrdinal("GroupName")),
                        reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? 0 : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    ));
                }
                reader.Close();

                foreach (var row in groupRows)
                {
                    var group = new Group
                    {
                        GroupId = row.GroupId,
                        GroupName = row.GroupName,
                        CreatedBy = row.CreatedBy,
                        CreatedAt = row.CreatedAt,
                        MemberIds = new List<int>()
                    };

                    string memberSql = "SELECT UserId FROM GroupMembers WHERE GroupId = @GroupId";
                    using var mCmd = new SqlCommand(memberSql, conn);
                    mCmd.Parameters.AddWithValue("@GroupId", row.GroupId);
                    using var mReader = mCmd.ExecuteReader();
                    while (mReader.Read())
                    {
                        group.MemberIds.Add(mReader.GetInt32(0));
                    }
                    mReader.Close();

                    list.Add(group);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi lấy danh sách nhóm của user: {ex.Message}");
            }
            return list;
        }

        public bool AddGroupMember(int groupId, int memberId)
        {
            if (groupId <= 0 || memberId <= 0) return false;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM GroupMembers
                        WHERE GroupId = @GroupId AND UserId = @UserId
                    )
                    BEGIN
                        INSERT INTO GroupMembers (GroupId, UserId, JoinedAt)
                        VALUES (@GroupId, @UserId, GETDATE())
                    END";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                cmd.Parameters.AddWithValue("@UserId", memberId);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi thêm thành viên: {ex.Message}");
                return false;
            }
        }

        public bool RemoveGroupMember(int groupId, int memberId)
        {
            if (groupId <= 0 || memberId <= 0) return false;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = @"
                    DELETE FROM GroupMembers
                    WHERE GroupId = @GroupId AND UserId = @UserId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                cmd.Parameters.AddWithValue("@UserId", memberId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi xóa thành viên: {ex.Message}");
                return false;
            }
        }

        public bool DeleteGroup(int groupId)
        {
            if (groupId <= 0) return false;

            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = "DELETE FROM Groups WHERE GroupId = @GroupId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@GroupId", groupId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi giải tán nhóm: {ex.Message}");
                return false;
            }
        }

        // Lấy toàn bộ nhóm trong CSDL
        public List<Group> GetAllGroups()
        {
            var list = new List<Group>();
            try
            {
                using var conn = GetConnection();
                conn.Open();

                string sql = "SELECT GroupId, GroupName, CreatedBy, CreatedAt FROM Groups ORDER BY GroupName ASC";
                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                var groupRows = new List<(int GroupId, string GroupName, int CreatedBy, DateTime CreatedAt)>();
                while (reader.Read())
                {
                    groupRows.Add((
                        reader.GetInt32(reader.GetOrdinal("GroupId")),
                        reader.GetString(reader.GetOrdinal("GroupName")),
                        reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? 0 : reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    ));
                }
                reader.Close();

                foreach (var row in groupRows)
                {
                    var group = new Group
                    {
                        GroupId = row.GroupId,
                        GroupName = row.GroupName,
                        CreatedBy = row.CreatedBy,
                        CreatedAt = row.CreatedAt,
                        MemberIds = new List<int>()
                    };

                    string memberSql = "SELECT UserId FROM GroupMembers WHERE GroupId = @GroupId";
                    using var mCmd = new SqlCommand(memberSql, conn);
                    mCmd.Parameters.AddWithValue("@GroupId", row.GroupId);
                    using var mReader = mCmd.ExecuteReader();
                    while (mReader.Read())
                    {
                        group.MemberIds.Add(mReader.GetInt32(0));
                    }
                    mReader.Close();

                    list.Add(group);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Lỗi lấy toàn bộ nhóm từ CSDL: {ex.Message}");
            }
            return list;
        }
    }
}
