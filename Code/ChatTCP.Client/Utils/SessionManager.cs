using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ChatTCP.Client.Network;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;
using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Utils
{
    // Quản lý phiên đăng nhập, cuộc trò chuyện và bộ nhớ lịch sử chat
    public class SessionManager
    {
        private static readonly Lazy<SessionManager> _instance = new Lazy<SessionManager>(() => new SessionManager());
        public static SessionManager Instance => _instance.Value;

        private readonly object _historyLock = new object();

        // Thông tin người dùng hiện tại
        public User? CurrentUser { get; private set; }
        public int CurrentUserId => CurrentUser?.UserId ?? 0;
        public string CurrentUsername => CurrentUser?.Username ?? string.Empty;
        public bool IsLoggedIn => CurrentUser != null;

        // Trạng thái cuộc trò chuyện đang mở
        public string ActiveChatTarget { get; private set; } = string.Empty;
        public int? ActiveTargetUserId { get; private set; }
        public bool IsActiveChatGroup { get; private set; } = false;
        public int? ActiveGroupId { get; private set; }

        // Bộ nhớ cache tin nhắn: "direct:{username}" hoặc "group:id:{id}"
        private readonly ConcurrentDictionary<string, List<ChatMessage>> _chatHistories =
            new ConcurrentDictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);

        // Đánh dấu đoạn chat đã tải lịch sử từ Server
        private readonly ConcurrentDictionary<string, bool> _loadedHistories =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Danh sách user online và nhóm
        private readonly ConcurrentDictionary<string, User> _onlineUsers =
            new ConcurrentDictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Group> _groups =
            new ConcurrentDictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

        // Sự kiện thông báo
        public event Action<User?>? SessionChanged;
        public event Action<string, bool>? ActiveChatChanged;
        public event Action<string, ChatMessage, bool>? MessageAdded;
        public event Action<string, IReadOnlyList<ChatMessage>>? HistoryUpdated;
        public event Action<string, bool>? UserStatusChanged;
        public event Action<Group>? GroupUpdated;

        public SessionManager()
        {
        }

        // QUẢN LÝ PHIÊN

        // Lưu thông tin người dùng khi đăng nhập thành công
        public void SetCurrentUser(User user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            SessionChanged?.Invoke(CurrentUser);
        }

        // Kết thúc phiên, mặc định giữ lại lịch sử chat cache (clearHistory = false)
        public void ClearSession(bool clearHistory = false)
        {
            CurrentUser = null;
            ActiveChatTarget = string.Empty;
            ActiveTargetUserId = null;
            IsActiveChatGroup = false;
            ActiveGroupId = null;

            if (clearHistory)
            {
                lock (_historyLock)
                {
                    _chatHistories.Clear();
                }
                _loadedHistories.Clear();
            }

            _onlineUsers.Clear();
            _groups.Clear();

            SessionChanged?.Invoke(null);
        }

        // CHỌN CUỘC TRÒ CHUYỆN & LẤY LỊCH SỬ TỪ SERVER

        // Mở chat 1-1 và tự động gửi yêu cầu lấy lịch sử từ Server
        public void SetActiveDirectChat(string targetUsername, int? targetUserId = null, TcpClientManager? tcpClient = null)
        {
            if (string.IsNullOrWhiteSpace(targetUsername)) return;

            ActiveChatTarget = targetUsername.Trim();
            ActiveTargetUserId = targetUserId;
            IsActiveChatGroup = false;
            ActiveGroupId = null;

            ActiveChatChanged?.Invoke(ActiveChatTarget, IsActiveChatGroup);

            if (tcpClient != null && tcpClient.IsConnected)
            {
                RequestHistoryFromServer(tcpClient);
            }
        }

        // Mở chat Nhóm và tự động gửi yêu cầu lấy lịch sử từ Server
        public void SetActiveGroupChat(string groupName, int? groupId = null, TcpClientManager? tcpClient = null)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;

            ActiveChatTarget = groupName.Trim();
            IsActiveChatGroup = true;
            ActiveGroupId = groupId;
            ActiveTargetUserId = null;

            ActiveChatChanged?.Invoke(ActiveChatTarget, IsActiveChatGroup);

            if (tcpClient != null && tcpClient.IsConnected)
            {
                RequestHistoryFromServer(tcpClient);
            }
        }

        // Gửi yêu cầu lấy lịch sử từ Database Server cho cuộc trò chuyện hiện tại
        public void RequestHistoryFromServer(TcpClientManager tcpClient, int limit = 100)
        {
            if (tcpClient == null || !tcpClient.IsConnected || string.IsNullOrEmpty(ActiveChatTarget))
            {
                return;
            }

            try
            {
                if (IsActiveChatGroup)
                {
                    int gId = ActiveGroupId ?? 0;
                    var request = new GroupHistoryRequest
                    {
                        GroupId = gId,
                        Limit = limit
                    };

                    var msg = new ChatMessage
                    {
                        SenderId = CurrentUserId,
                        SenderName = CurrentUsername,
                        GroupId = gId > 0 ? gId : null,
                        Type = MessageType.GetChatHistoryRequest,
                        Content = JsonSerializer.Serialize(request),
                        Timestamp = DateTime.Now
                    };

                    tcpClient.SendMessage(msg);
                }
                else
                {
                    var msg = new ChatMessage
                    {
                        SenderId = CurrentUserId,
                        SenderName = CurrentUsername,
                        ReceiverId = ActiveTargetUserId,
                        Type = MessageType.GetChatHistoryRequest,
                        Content = ActiveChatTarget,
                        Timestamp = DateTime.Now
                    };

                    tcpClient.SendMessage(msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SessionManager] Lỗi gửi yêu cầu lịch sử: {ex.Message}");
            }
        }

        // Xóa lựa chọn cuộc trò chuyện
        public void ClearActiveChat()
        {
            ActiveChatTarget = string.Empty;
            ActiveTargetUserId = null;
            IsActiveChatGroup = false;
            ActiveGroupId = null;

            ActiveChatChanged?.Invoke(string.Empty, false);
        }

        // Lấy key định danh của cuộc trò chuyện đang mở
        public string GetActiveConversationKey()
        {
            if (string.IsNullOrEmpty(ActiveChatTarget)) return string.Empty;

            if (IsActiveChatGroup)
            {
                return ActiveGroupId.HasValue && ActiveGroupId.Value > 0
                    ? GetGroupKey(ActiveGroupId.Value)
                    : GetGroupKey(ActiveChatTarget);
            }

            return GetDirectChatKey(ActiveChatTarget);
        }

        // Tạo key chuẩn hóa
        public static string GetDirectChatKey(string username) => $"direct:{username.Trim().ToLowerInvariant()}";
        public static string GetGroupKey(int groupId) => $"group:id:{groupId}";
        public static string GetGroupKey(string groupName) => $"group:name:{groupName.Trim().ToLowerInvariant()}";

        //  QUẢN LÝ LỊCH SỬ TIN NHẮN 

        // Thêm 1 tin nhắn mới vào đúng cuộc trò chuyện
        public void AddMessage(ChatMessage message)
        {
            if (message == null) return;

            string convKey = ResolveConversationKey(message);
            if (string.IsNullOrEmpty(convKey)) return;

            bool isCurrentChat = IsCurrentConversation(message, convKey);

            lock (_historyLock)
            {
                var history = _chatHistories.GetOrAdd(convKey, _ => new List<ChatMessage>());

                // Tránh thêm trùng lặp
                if (message.Id > 0 && history.Any(m => m.Id == message.Id))
                {
                    return;
                }

                history.Add(message);
            }

            MessageAdded?.Invoke(convKey, message, isCurrentChat);
        }

        // Thêm tin nhắn 1-1 chỉ định rõ người nhận (dùng khi chuyển tiếp)
        public void AddDirectMessage(string targetUsername, ChatMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(targetUsername)) return;
            string convKey = GetDirectChatKey(targetUsername);
            bool isCurrentChat = !IsActiveChatGroup && string.Equals(ActiveChatTarget, targetUsername, StringComparison.OrdinalIgnoreCase);

            lock (_historyLock)
            {
                var history = _chatHistories.GetOrAdd(convKey, _ => new List<ChatMessage>());
                if (message.Id > 0 && history.Any(m => m.Id == message.Id))
                {
                    return;
                }

                history.Add(message);
            }

            MessageAdded?.Invoke(convKey, message, isCurrentChat);
        }

        // Thêm tin nhắn nhóm chỉ định rõ nhóm (dùng khi chuyển tiếp)
        public void AddGroupMessage(string groupName, int? groupId, ChatMessage message)
        {
            if (message == null) return;
            string convKey = groupId.HasValue && groupId.Value > 0 ? GetGroupKey(groupId.Value) : GetGroupKey(groupName);
            bool isCurrentChat = IsActiveChatGroup && (
                (groupId.HasValue && ActiveGroupId.HasValue && groupId.Value == ActiveGroupId.Value) ||
                string.Equals(ActiveChatTarget, groupName, StringComparison.OrdinalIgnoreCase));

            lock (_historyLock)
            {
                var history = _chatHistories.GetOrAdd(convKey, _ => new List<ChatMessage>());
                if (message.Id > 0 && history.Any(m => m.Id == message.Id))
                {
                    return;
                }

                history.Add(message);
            }

            MessageAdded?.Invoke(convKey, message, isCurrentChat);
        }

        // Gán lại toàn bộ lịch sử (từ Server trả về)
        public void SetHistory(string conversationKey, IEnumerable<ChatMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(conversationKey) || messages == null) return;

            List<ChatMessage> list;
            lock (_historyLock)
            {
                list = messages.OrderBy(m => m.Timestamp).ToList();
                _chatHistories[conversationKey] = list;
                _loadedHistories[conversationKey] = true;
            }

            HistoryUpdated?.Invoke(conversationKey, list);
        }

        // Nạp thêm lịch sử từ Server, tự lọc trùng tin nhắn cũ
        public void AppendHistory(string conversationKey, IEnumerable<ChatMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(conversationKey) || messages == null) return;

            List<ChatMessage> resultList;
            lock (_historyLock)
            {
                var history = _chatHistories.GetOrAdd(conversationKey, _ => new List<ChatMessage>());
                foreach (var msg in messages)
                {
                    bool exists = (msg.Id > 0 && history.Any(m => m.Id == msg.Id))
                        || history.Any(m => m.Timestamp == msg.Timestamp && m.SenderName == msg.SenderName && m.Content == msg.Content);

                    if (!exists)
                    {
                        history.Add(msg);
                    }
                }

                history.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                resultList = history.ToList();
                _loadedHistories[conversationKey] = true;
            }

            HistoryUpdated?.Invoke(conversationKey, resultList);
        }

        // Lấy lịch sử chat 1-1 với user
        public IReadOnlyList<ChatMessage> GetDirectChatHistory(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return Array.Empty<ChatMessage>();

            string key = GetDirectChatKey(username);
            lock (_historyLock)
            {
                if (_chatHistories.TryGetValue(key, out var list))
                {
                    return list.ToList();
                }
            }
            return Array.Empty<ChatMessage>();
        }

        // Lấy lịch sử chat nhóm theo GroupId
        public IReadOnlyList<ChatMessage> GetGroupChatHistory(int groupId)
        {
            string key = GetGroupKey(groupId);
            lock (_historyLock)
            {
                if (_chatHistories.TryGetValue(key, out var list))
                {
                    return list.ToList();
                }
            }
            return Array.Empty<ChatMessage>();
        }

        // Lấy lịch sử chat nhóm theo tên nhóm
        public IReadOnlyList<ChatMessage> GetGroupChatHistory(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return Array.Empty<ChatMessage>();

            string key = GetGroupKey(groupName);
            lock (_historyLock)
            {
                if (_chatHistories.TryGetValue(key, out var list))
                {
                    return list.ToList();
                }
            }
            return Array.Empty<ChatMessage>();
        }

        // Lấy lịch sử cuộc trò chuyện đang mở
        public IReadOnlyList<ChatMessage> GetCurrentChatHistory()
        {
            string key = GetActiveConversationKey();
            if (string.IsNullOrEmpty(key)) return Array.Empty<ChatMessage>();

            lock (_historyLock)
            {
                if (_chatHistories.TryGetValue(key, out var list))
                {
                    return list.ToList();
                }
            }
            return Array.Empty<ChatMessage>();
        }

        // Kiểm tra lịch sử đã được nạp từ Server chưa
        public bool IsHistoryLoaded(string conversationKey)
        {
            return _loadedHistories.TryGetValue(conversationKey, out bool loaded) && loaded;
        }

        // QUẢN LÝ USER ONLINE & NHÓM 

        // Cập nhật trạng thái user
        public void UpdateUserStatus(string username, bool isOnline)
        {
            if (string.IsNullOrWhiteSpace(username)) return;

            var user = _onlineUsers.GetOrAdd(username, name => new User
            {
                Username = name,
                DisplayName = name,
                Status = isOnline ? "Online" : "Offline"
            });

            user.Status = isOnline ? "Online" : "Offline";
            UserStatusChanged?.Invoke(username, isOnline);
        }

        // Cập nhật toàn bộ danh sách user online
        public void SetOnlineUsers(IEnumerable<User> users)
        {
            if (users == null) return;

            _onlineUsers.Clear();
            foreach (var u in users)
            {
                _onlineUsers[u.Username] = u;
                UserStatusChanged?.Invoke(u.Username, string.Equals(u.Status, "Online", StringComparison.OrdinalIgnoreCase));
            }
        }

        // Lấy danh sách user online
        public IReadOnlyList<User> GetOnlineUsers()
        {
            return _onlineUsers.Values.ToList();
        }

        // Thêm hoặc cập nhật nhóm
        public void AddOrUpdateGroup(Group group)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.GroupName)) return;

            _groups[group.GroupName] = group;
            GroupUpdated?.Invoke(group);
        }

        // Lấy danh sách nhóm
        public IReadOnlyList<Group> GetGroups()
        {
            return _groups.Values.ToList();
        }

        // HÀM NỘI BỘ 

        // Phân loại tin nhắn vào đúng key cuộc trò chuyện
        private string ResolveConversationKey(ChatMessage message)
        {
            if (message.Type == MessageType.GroupChat || message.GroupId.HasValue)
            {
                if (message.GroupId.HasValue && message.GroupId.Value > 0)
                {
                    return GetGroupKey(message.GroupId.Value);
                }

                if (IsActiveChatGroup && !string.IsNullOrEmpty(ActiveChatTarget))
                {
                    return GetGroupKey(ActiveChatTarget);
                }

                return "group:general";
            }

            // Tin nhắn 1-1
            if (string.Equals(message.SenderName, CurrentUsername, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsActiveChatGroup && !string.IsNullOrEmpty(ActiveChatTarget))
                {
                    return GetDirectChatKey(ActiveChatTarget);
                }
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(message.SenderName))
            {
                return GetDirectChatKey(message.SenderName);
            }

            return string.Empty;
        }

        // Kiểm tra tin nhắn có thuộc cuộc trò chuyện đang mở không
        private bool IsCurrentConversation(ChatMessage message, string convKey)
        {
            if (string.IsNullOrEmpty(ActiveChatTarget)) return false;

            if (IsActiveChatGroup)
            {
                if (message.GroupId.HasValue && ActiveGroupId.HasValue)
                {
                    return message.GroupId.Value == ActiveGroupId.Value;
                }
                return convKey == GetGroupKey(ActiveChatTarget);
            }

            string senderOrPartner = string.Equals(message.SenderName, CurrentUsername, StringComparison.OrdinalIgnoreCase)
                ? ActiveChatTarget
                : message.SenderName;

            return string.Equals(senderOrPartner, ActiveChatTarget, StringComparison.OrdinalIgnoreCase);
        }
    }
}
