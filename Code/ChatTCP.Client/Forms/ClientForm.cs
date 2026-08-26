using ChatTCP.Client.Network;
using ChatTCP.Client.Services;
using ChatTCP.Client.UserControls;
using ChatTCP.Client.Utils;
using ChatTCP.Shared.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChatMessage = ChatTCP.Shared.Models.Message;
using ChatTCP.Shared.Enums;



namespace ChatTCP.Client.Forms
{
    public partial class ClientForm : Form
    {

        private TcpClientManager _tcpClient = null!;
        private ChatService _chatService = null!;

        private GroupService? _groupService;

        private EmojiService _emojiService = null!;

        private readonly User _currentUser;
        private readonly string _currentUsername;
        private string _activeChatTarget = "";
        private bool _activeChatIsGroup = false;
        private readonly Dictionary<string, User> _onlineUsersByName = new Dictionary<string, User>();
        private int _nextDemoUserId = 1;

        // UI Controls - Header 
        private Panel pnlHeader = null!;
        private Label lblMyAvatar = null!;
        private Label lblMyUsername = null!;
        private Label lblMyStatus = null!;

        // UI Controls - Sidebar (trái) 
        private Panel pnlSidebar = null!;
        private TabControl tabSidebar = null!;
        private TabPage tabUsers = null!;
        private TabPage tabGroups = null!;
        private ListView lvUsers = null!;
        private ListView lvGroups = null!;
        private Button btnCreateGroup = null!;
        private ContextMenuStrip cmsUsers = null!;
        private ToolStripMenuItem miStartChat = null!;

        // UI Controls - Khung chat (phải) 
        private Panel pnlChat = null!;
        private Panel pnlChatHeader = null!;
        private Label lblChatTarget = null!;
        private Label lblChatStatus = null!;

        private Panel pnlMessages = null!;
        private FlowLayoutPanel flpMessages = null!;

        private Panel pnlInput = null!;
        private Button btnEmoji = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;

        // Lưu tham chiếu các item đang hiển thị (mô phỏng, sẽ thay bằng dữ liệu thật từ backend)
        private readonly Dictionary<string, ListViewItem> _userItems = new Dictionary<string, ListViewItem>();
        private readonly Dictionary<string, ListViewItem> _groupItems = new Dictionary<string, ListViewItem>();

        public ClientForm(User currentUser)
        {
            _currentUser = currentUser;
            _currentUsername = currentUser.Username;
            InitializeComponent();
            InitializeClientComponents();
        }

        // UI SETUP 
        private void InitializeComponent()
        {
            this.Text = "ChatTCP - Client";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);

            //  Header trên cùng 
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(30, 90, 180),
                Padding = new Padding(12, 0, 12, 0)
            };

            lblMyAvatar = new Label
            {
                Text = GetInitials(_currentUsername),
                Size = new Size(30, 30),
                Location = new Point(12, 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 90, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            MakeCircle(lblMyAvatar);

            lblMyUsername = new Label
            {
                Text = _currentUsername,
                Location = new Point(52, 8),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblMyStatus = new Label
            {
                Text = "● Online",
                Location = new Point(52, 26),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 255, 200),
                Font = new Font("Segoe UI", 8F)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblMyAvatar, lblMyUsername, lblMyStatus });

            // Context menu cho danh sách User 
            cmsUsers = new ContextMenuStrip();
            miStartChat = new ToolStripMenuItem("Nhắn tin");
            miStartChat.Click += MiStartChat_Click;
            cmsUsers.Items.Add(miStartChat);

            // Sidebar bên trái: Users / Groups 
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 220 };

            tabSidebar = new TabControl { Dock = DockStyle.Fill };

            tabUsers = new TabPage("Users");
            lvUsers = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.None,
                ContextMenuStrip = cmsUsers
            };
            lvUsers.Columns.Add("User", 216);
            lvUsers.MouseDown += LvUsers_MouseDown;
            lvUsers.DoubleClick += (s, e) => OpenChatWithUser();
            tabUsers.Controls.Add(lvUsers);

            tabGroups = new TabPage("Groups");
            lvGroups = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.None
            };
            lvGroups.Columns.Add("Group", 216);
            lvGroups.DoubleClick += (s, e) => OpenChatWithGroup();
            tabGroups.Controls.Add(lvGroups);

            tabSidebar.TabPages.Add(tabUsers);
            tabSidebar.TabPages.Add(tabGroups);

            btnCreateGroup = new Button
            {
                Text = "+ Tạo nhóm mới",
                Dock = DockStyle.Bottom,
                Height = 32,
                FlatStyle = FlatStyle.Flat
            };
            btnCreateGroup.Click += BtnCreateGroup_Click;

            pnlSidebar.Controls.Add(tabSidebar);
            pnlSidebar.Controls.Add(btnCreateGroup);

            // Khung chat bên phải 
            pnlChat = new Panel { Dock = DockStyle.Fill };

            // Header của khung chat: tên người/nhóm đang chat
            pnlChatHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(12, 0, 12, 0)
            };
            lblChatTarget = new Label
            {
                Text = "Chọn một cuộc trò chuyện",
                Location = new Point(12, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            lblChatStatus = new Label
            {
                Text = "",
                Location = new Point(12, 25),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            pnlChatHeader.Controls.AddRange(new Control[] { lblChatTarget, lblChatStatus });

            // Khu vực hiển thị tin nhắn (cuộn được)
            pnlMessages = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            flpMessages = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12)
            };
            pnlMessages.Controls.Add(flpMessages);

            // Thanh nhập tin nhắn phía dưới
            pnlInput = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8) };

            btnEmoji = new Button
            {
                Text = "😊",
                Location = new Point(8, 8),
                Width = 36,
                Height = 34,
                FlatStyle = FlatStyle.Flat
            };
            btnEmoji.Click += BtnEmoji_Click;

            btnSend = new Button
            {
                Text = "Gửi",
                Width = 70,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 90, 180),
                ForeColor = Color.White
            };
            btnSend.Click += BtnSend_Click;

            txtMessage = new TextBox
            {
                Location = new Point(52, 10),
                Height = 34,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Nhập tin nhắn..."
            };
            txtMessage.KeyDown += TxtMessage_KeyDown;

            pnlInput.Controls.AddRange(new Control[] { btnEmoji, txtMessage, btnSend });
            pnlInput.Resize += (s, e) => LayoutInputBar();

            pnlChat.Controls.Add(pnlMessages);
            pnlChat.Controls.Add(pnlInput);
            pnlChat.Controls.Add(pnlChatHeader);

            // Add to form (thứ tự dock quan trọng)
            this.Controls.Add(pnlChat);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlHeader);

            this.Load += (s, e) => LayoutInputBar();
            this.FormClosing += ClientForm_FormClosing;
        }

        // LOGIC SETUP
        private void InitializeClientComponents()
        {
            // Thiết lập phiên đăng nhập cho người dùng hiện tại
            SessionManager.Instance.SetCurrentUser(_currentUser);

            // Lắng nghe sự kiện khi lịch sử chat được nạp hoặc cập nhật từ Server
            SessionManager.Instance.HistoryUpdated += OnHistoryUpdated;

            //  Khởi tạo TcpClientManager / ChatService 
            _tcpClient = new TcpClientManager();
            _chatService = new ChatService(_tcpClient);
            _chatService.OnMessageReceived += ChatService_OnMessageReceived;

            // Khởi tạo GroupService
            _groupService = new GroupService(_tcpClient, _currentUser.UserId);

            // Khởi tạo emoji
            _emojiService = new EmojiService();

            var onlineUsers = SessionManager.Instance.GetOnlineUsers();
            foreach (var u in onlineUsers) AddUserToList(u.Username, string.Equals(u.Status, "Online", StringComparison.OrdinalIgnoreCase));

            // ---- Demo tạm thời để xem trước giao diện (xóa khi có backend thật) ----
            AddUserToList("an_nguyen", true);
            AddUserToList("minh_le", true);
            AddUserToList("thu_tran", false);
            AddGroupToList("Nhóm Đồ Án UDM08");
        }

        //  EVENT HANDLERS - SIDEBAR 
        private void LvUsers_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item = lvUsers.GetItemAt(e.X, e.Y);
                if (item != null) item.Selected = true;
            }
        }

        private void MiStartChat_Click(object? sender, EventArgs e) => OpenChatWithUser();

        private void OpenChatWithUser()
        {
            if (lvUsers.SelectedItems.Count == 0) return;

            string username = lvUsers.SelectedItems[0].Tag as string ?? lvUsers.SelectedItems[0].Text;
            _activeChatTarget = username;
            _activeChatIsGroup = false;

            lblChatTarget.Text = username;
            lblChatStatus.Text = "Chat 1-1";

            // 1. Chuyển phiên chat sang User này và tự động gửi request lấy lịch sử từ Database Server (nếu đã kết nối TCP)
            SessionManager.Instance.SetActiveDirectChat(username, tcpClient: _tcpClient);

            // 2. Hiển thị ngay lịch sử đã có sẵn trong bộ nhớ cache
            flpMessages.Controls.Clear();
            var cachedHistory = SessionManager.Instance.GetDirectChatHistory(username);
            foreach (var msg in cachedHistory)
            {
                AddMessageBubble(msg, msg.SenderName == _currentUsername);
            }
        }

        private void OpenChatWithGroup()
        {
            if (lvGroups.SelectedItems.Count == 0) return;

            string groupName = lvGroups.SelectedItems[0].Text;
            _activeChatTarget = groupName;
            _activeChatIsGroup = true;

            lblChatTarget.Text = groupName;
            lblChatStatus.Text = "Nhóm chat";

            // 1. Chuyển phiên chat sang Nhóm và tự động gửi request lấy lịch sử nhóm từ Server
            SessionManager.Instance.SetActiveGroupChat(groupName, groupId: null, tcpClient: _tcpClient);

            // 2. Hiển thị ngay lịch sử nhóm đã có sẵn trong bộ nhớ cache
            flpMessages.Controls.Clear();
            var cachedHistory = SessionManager.Instance.GetGroupChatHistory(groupName);
            foreach (var msg in cachedHistory)
            {
                AddMessageBubble(msg, msg.SenderName == _currentUsername);
            }
        }

        private void BtnCreateGroup_Click(object? sender, EventArgs e)
        {
            if (_groupService == null)
            {
                MessageBox.Show(
                    "Chức năng tạo nhóm sẽ hoạt động khi GroupService được tích hợp.",
                    "Tạo nhóm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var createGroupForm = new CreateGroupForm(_groupService, _onlineUsersByName.Values))
            {
                if (createGroupForm.ShowDialog(this) == DialogResult.OK
                    && createGroupForm.CreatedGroup != null)
                {
                    AddGroupToList(createGroupForm.CreatedGroup.GroupName);
                }
            }
        }

        //  EVENT HANDLERS - GỬI TIN NHẮN 
        private void TxtMessage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                SendCurrentMessage();
            }
        }

        private void BtnSend_Click(object? sender, EventArgs e) => SendCurrentMessage();

        private void SendCurrentMessage()
        {
            string content = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            if (string.IsNullOrEmpty(_activeChatTarget))
            {
                MessageBox.Show("Vui lòng chọn một người dùng hoặc nhóm để chat.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = new ChatMessage
            {
                SenderId = _currentUser.UserId,
                SenderName = _currentUsername,
                Content = content,
                Timestamp = DateTime.Now
            };

            if (_activeChatIsGroup)
            {
                message.Type = MessageType.GroupChat;
                int? groupId = SessionManager.Instance.ActiveGroupId;
                if (groupId.HasValue && groupId.Value > 0 && _groupService != null)
                {
                    message.GroupId = groupId.Value;
                    _groupService.SendGroupMessage(groupId.Value, content);
                }
                else
                {
                    _chatService.SendMessage(message);
                }
            }
            else
            {
                message.Type = MessageType.DirectChat;
                if (_onlineUsersByName.TryGetValue(_activeChatTarget, out var targetUser))
                {
                    message.ReceiverId = targetUser.UserId;
                }
                _chatService.SendMessage(message);
            }

            // Lưu tin nhắn gửi đi vào SessionManager
            SessionManager.Instance.AddMessage(message);

            AddMessageBubble(message, isMine: true);
            txtMessage.Clear();
            txtMessage.Focus();
        }

        private void BtnEmoji_Click(object? sender, EventArgs e)
        {
            using (var picker = new EmojiPickerForm())
            {
                if (picker.ShowDialog(this) == DialogResult.OK
                    && !string.IsNullOrEmpty(picker.SelectedEmoji))
                {
                    txtMessage.Text += picker.SelectedEmoji;
                    txtMessage.SelectionStart = txtMessage.Text.Length;
                    txtMessage.Focus();
                }
            }
        }

        private void ClientForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SessionManager.Instance.HistoryUpdated -= OnHistoryUpdated;
            if (_chatService != null)
            {
                _chatService.OnMessageReceived -= ChatService_OnMessageReceived;
                _chatService.Dispose();
            }
            _groupService?.Dispose();

            // Ngắt kết nối khỏi Server an toàn trước khi thoát
            _tcpClient?.Disconnect();
            Application.Exit();
        }

        // Cập nhật lại danh sách bong bóng chat khi có dữ liệu lịch sử mới từ Server
        private void OnHistoryUpdated(string conversationKey, IReadOnlyList<ChatMessage> messages)
        {
            InvokeIfRequired(() =>
            {
                if (conversationKey == SessionManager.Instance.GetActiveConversationKey())
                {
                    flpMessages.Controls.Clear();
                    foreach (var msg in messages)
                    {
                        AddMessageBubble(msg, msg.SenderName == _currentUsername);
                    }
                }
            });
        }


        // Event handler khi ChatService nhận tin nhắn trực tiếp 1-1
        private void ChatService_OnMessageReceived(ChatMessage message)
        {
            OnMessageReceived(message, isGroupMessage: false);
        }

        //  Gọi khi nhận tin nhắn mới (1-1 hoặc Group) từ Server. 
        public void OnMessageReceived(ChatMessage message, bool isGroupMessage)
        {
            InvokeIfRequired(() =>
            {
                // Lưu vào SessionManager để quản lý tập trung và chống trùng lặp
                SessionManager.Instance.AddMessage(message);

                bool isCurrentChat = message.SenderName == _activeChatTarget && isGroupMessage == _activeChatIsGroup;
                if (isCurrentChat)
                {
                    AddMessageBubble(message, isMine: false);
                }

                // Hiện thông báo / đánh dấu chưa đọc nếu không phải đoạn chat đang mở
            });
        }


        // Gọi khi trạng thái Online/Offline của 1 User thay đổi

        public void OnUserStatusChanged(string username, bool isOnline)
        {
            InvokeIfRequired(() =>
            {
                SessionManager.Instance.UpdateUserStatus(username, isOnline);

                if (_userItems.TryGetValue(username, out var item))
                {
                    UpdateUserStatus(item, isOnline);
                }
                else
                {
                    AddUserToList(username, isOnline);
                }
            });
        }
        // Gọi khi có nhóm mới hoặc được thêm vào nhóm
        public void OnGroupUpdated(string groupName)
        {
            InvokeIfRequired(() => AddGroupToList(groupName));
        }

        // HELPERS - SIDEBAR LIST
        private void AddUserToList(string username, bool isOnline)
        {
            if (_userItems.ContainsKey(username))
            {
                UpdateUserStatus(_userItems[username], isOnline);
                return;
            }

            var item = new ListViewItem(username) { Tag = username };
            item.ForeColor = isOnline ? Color.Black : Color.Gray;
            lvUsers.Items.Add(item);
            _userItems[username] = item;

            if (!_onlineUsersByName.ContainsKey(username))
            {
                _onlineUsersByName[username] = new User
                {
                    UserId = _nextDemoUserId++,
                    Username = username,
                    DisplayName = username
                };
            }
        }

        private void UpdateUserStatus(ListViewItem item, bool isOnline)
        {
            item.ForeColor = isOnline ? Color.Black : Color.Gray;
        }

        private void AddGroupToList(string groupName)
        {
            if (_groupItems.ContainsKey(groupName)) return;

            var item = new ListViewItem(groupName);
            lvGroups.Items.Add(item);
            _groupItems[groupName] = item;
        }

        // HELPERS - CHAT BUBBLE 
        private void AddMessageBubble(ChatMessage message, bool isMine)
        {
            var bubble = new ChatBubble
            {
                Margin = new Padding(isMine ? 100 : 4, 4, isMine ? 4 : 100, 4)
            };
            bubble.SetMessage(message, isMine);
            bubble.ReplyClicked += ChatBubble_ReplyClicked;

            flpMessages.Controls.Add(bubble);
            flpMessages.SetFlowBreak(bubble, true);

            ScrollMessagesToBottom();
        }

        // Gọi khi người dùng bấm "Trả lời" trên 1 ChatBubble
        private void ChatBubble_ReplyClicked(object? sender, ChatMessage repliedMessage)
        {
            MessageBox.Show(
                $"Đang trả lời: \"{repliedMessage.Content}\"\n(Chức năng gửi kèm Reply sẽ hoàn thiện khi ChatService tích hợp.)",
                "Trả lời tin nhắn", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ScrollMessagesToBottom()
        {
            pnlMessages.VerticalScroll.Value = pnlMessages.VerticalScroll.Maximum;
            pnlMessages.PerformLayout();
        }

        // HELPERS - KHÁC 
        private void LayoutInputBar()
        {
            btnSend.Location = new Point(pnlInput.Width - btnSend.Width - 8, 8);
            txtMessage.Width = pnlInput.Width - btnEmoji.Width - btnSend.Width - 28;
        }

        private static string GetInitials(string username)
        {
            if (string.IsNullOrEmpty(username)) return "?";
            return username.Length >= 2
                ? username.Substring(0, 2).ToUpper()
                : username.Substring(0, 1).ToUpper();
        }

        private static void MakeCircle(Label label)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, label.Width, label.Height);
            label.Region = new Region(path);
        }

        private void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
    }
}