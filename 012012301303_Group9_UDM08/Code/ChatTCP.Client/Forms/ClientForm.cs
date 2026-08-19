using ChatTCP.Client.Services;
using ChatTCP.Client.UserControls;
using ChatTCP.Shared.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChatMessage = ChatTCP.Shared.Models.Message;

// Tạm thời comment các thư viện backend chưa có code
// Khi các thành viên khác hoàn thành, bỏ comment và nối vào các điểm TODO bên dưới
// using ChatTCP.Client.Network;
// using ChatTCP.Shared.Enums;
//
// GroupService: dùng ChatTCP.Client.Services.GroupService (file thật của TV3).
// Contract thật: TcpClientManager tcpClientManager, int CurrentUserId trong constructor;
// event Action<Group> GroupCreated; event Action<string> CreateGroupFailed;
// void RequestCreateGroup(string groupName, IEnumerable<int> memberIds); IDisposable.
// (Không có event OnGroupUpdated - đã bỏ phần subscribe tương ứng.)

namespace ChatTCP.Client.Forms
{
    public partial class ClientForm : Form
    {
        // Tạm thời ẩn các biến backend, sẽ khai báo thật khi có code từ các TV khác
        // private TcpClientManager _tcpClient;    // [TV2]
        // private ChatService _chatService;        // [TV2]

        // [TV3] Khai báo sẵn, khởi tạo ở InitializeClientComponents() khi GroupService
        // thật đã sẵn sàng. Để null cho tới lúc đó - BtnCreateGroup_Click sẽ tự chặn.
        private GroupService? _groupService;

        // private EmojiService _emojiService;        // [TV4] - đã có sẵn, dùng thẳng qua EmojiPickerForm
        // private SessionManager _session;            // [TV1]

        private readonly User _currentUser;
        private readonly string _currentUsername;
        private string _activeChatTarget = "";
        private bool _activeChatIsGroup = false;

        // Danh sách User đầy đủ (UserId, Username, DisplayName) đang online, dùng để
        // truyền vào CreateGroupForm. Được đồng bộ song song với _userItems.
        // TODO: [TV5] Khi OnUserStatusChanged(...) trả về đủ thông tin User thay vì
        // chỉ username, thay đoạn tạo User "demo" trong AddUserToList bằng dữ liệu thật.
        private readonly Dictionary<string, User> _onlineUsersByName = new Dictionary<string, User>();
        private int _nextDemoUserId = 1;

        // ==== UI Controls - Header ====
        // Các control này thực sự được khởi tạo trong InitializeComponent() (gọi từ
        // constructor) chứ không phải trong field initializer, nên trình biên dịch
        // không phân tích luồng được và báo CS8618. Gán "= null!;" để xác nhận với
        // compiler rằng field sẽ luôn được gán trước khi dùng (giống pattern Designer.cs).
        private Panel pnlHeader = null!;
        private Label lblMyAvatar = null!;
        private Label lblMyUsername = null!;
        private Label lblMyStatus = null!;

        // ==== UI Controls - Sidebar (trái) ====
        private Panel pnlSidebar = null!;
        private TabControl tabSidebar = null!;
        private TabPage tabUsers = null!;
        private TabPage tabGroups = null!;
        private ListView lvUsers = null!;
        private ListView lvGroups = null!;
        private Button btnCreateGroup = null!;
        private ContextMenuStrip cmsUsers = null!;
        private ToolStripMenuItem miStartChat = null!;

        // ==== UI Controls - Khung chat (phải) ====
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

        // ================== UI SETUP ==================
        private void InitializeComponent()
        {
            this.Text = "ChatTCP - Client";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);

            // ---- Header trên cùng ----
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

            // ---- Context menu cho danh sách User ----
            cmsUsers = new ContextMenuStrip();
            miStartChat = new ToolStripMenuItem("Nhắn tin");
            miStartChat.Click += MiStartChat_Click;
            cmsUsers.Items.Add(miStartChat);

            // ---- Sidebar bên trái: Users / Groups ----
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

            // ---- Khung chat bên phải ----
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

            // ---- Add to form (thứ tự dock quan trọng) ----
            this.Controls.Add(pnlChat);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlHeader);

            this.Load += (s, e) => LayoutInputBar();
            this.FormClosing += ClientForm_FormClosing;
        }

        // ================== LOGIC SETUP ==================
        private void InitializeClientComponents()
        {
            // TODO: [TV2] Khởi tạo TcpClientManager / ChatService khi các lớp đã sẵn sàng
            // _tcpClient = new TcpClientManager();
            // _chatService = new ChatService(_tcpClient);
            // _chatService.OnMessageReceived += ChatService_OnMessageReceived;

            // TODO: [TV2] Khởi tạo TcpClientManager trước, sau đó GroupService thật
            // mới dùng được (constructor cần TcpClientManager + CurrentUserId).
            // _tcpClient = new TcpClientManager();
            // _groupService = new GroupService(_tcpClient, _currentUser.UserId);
            //
            // Lưu ý: GroupService thật KHÔNG có event OnGroupUpdated (chỉ có
            // GroupCreated / CreateGroupFailed dùng cho luồng tạo nhóm), nên
            // không cần subscribe gì thêm ở đây - AddGroupToList đã được gọi
            // trực tiếp trong BtnCreateGroup_Click khi CreateGroupForm đóng lại
            // với DialogResult.OK.

            // TODO: [TV4] Khởi tạo EmojiService khi lớp EmojiService.cs đã sẵn sàng
            // _emojiService = new EmojiService();

            // TODO: [TV1] Lấy danh sách User online ban đầu từ SessionManager / Server
            // var onlineUsers = _session.GetOnlineUsers();
            // foreach (var u in onlineUsers) AddUserToList(u.Username, u.IsOnline);

            // ---- Demo tạm thời để xem trước giao diện (xóa khi có backend thật) ----
            AddUserToList("an_nguyen", true);
            AddUserToList("minh_le", true);
            AddUserToList("thu_tran", false);
            AddGroupToList("Nhóm Đồ Án UDM08");
        }

        // ================== EVENT HANDLERS - SIDEBAR ==================
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
            flpMessages.Controls.Clear();

            // TODO: [TV2] Tải lịch sử chat 1-1 từ ChatService/DatabaseService
            // var history = _chatService.GetHistory(_currentUsername, username);
            // foreach (var msg in history) AddMessageBubble(msg, msg.SenderName == _currentUsername);
        }

        private void OpenChatWithGroup()
        {
            if (lvGroups.SelectedItems.Count == 0) return;

            string groupName = lvGroups.SelectedItems[0].Text;
            _activeChatTarget = groupName;
            _activeChatIsGroup = true;

            lblChatTarget.Text = groupName;
            lblChatStatus.Text = "Nhóm chat";
            flpMessages.Controls.Clear();

            // TODO: [TV3] Tải lịch sử chat Group từ GroupService
            // var history = _groupService.GetHistory(groupName);
            // foreach (var msg in history) AddMessageBubble(msg, msg.SenderName == _currentUsername);
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
                    // CreateGroupForm tự gửi request và lắng nghe GroupCreated/CreateGroupFailed
                    // qua _groupService rồi mới đóng dialog, nên ở đây chỉ cần cập nhật UI.
                    AddGroupToList(createGroupForm.CreatedGroup.GroupName);
                }
            }
        }

        // ================== EVENT HANDLERS - GỬI TIN NHẮN ==================
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
                SenderName = _currentUsername,
                Content = content,
                Timestamp = DateTime.Now
            };

            // TODO: [TV2/TV3] Gửi tin nhắn thật qua ChatService (1-1) hoặc GroupService (Group)
            // if (_activeChatIsGroup)
            //     _groupService.SendMessage(_activeChatTarget, message);
            // else
            //     _chatService.SendMessage(_activeChatTarget, message);

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
            _groupService?.Dispose();

            // TODO: [TV2] Ngắt kết nối khỏi Server an toàn trước khi thoát
            // _tcpClient?.Disconnect();
            Application.Exit();
        }

        // ================== HOOKS TÍCH HỢP BACKEND ==================
        // Các hàm dưới đây được gọi từ backend (qua sự kiện) để cập nhật UI.
        // Khi tích hợp thật, nối các sự kiện của TcpClientManager/ChatService/GroupService vào đây.

        /// <summary>
        /// Gọi khi nhận tin nhắn mới (1-1 hoặc Group) từ Server. [Hook cho TV2/TV3]
        /// </summary>
        public void OnMessageReceived(ChatMessage message, bool isGroupMessage)
        {
            InvokeIfRequired(() =>
            {
                bool isCurrentChat = message.SenderName == _activeChatTarget && isGroupMessage == _activeChatIsGroup;
                if (isCurrentChat)
                {
                    AddMessageBubble(message, isMine: false);
                }

                // TODO: [TV4/TV5] Hiện thông báo / đánh dấu chưa đọc nếu không phải đoạn chat đang mở
            });
        }

        /// <summary>
        /// Gọi khi trạng thái Online/Offline của 1 User thay đổi. [Hook cho TV5]
        /// </summary>
        public void OnUserStatusChanged(string username, bool isOnline)
        {
            InvokeIfRequired(() =>
            {
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

        /// <summary>
        /// Gọi khi có nhóm mới hoặc được thêm vào nhóm. [Hook cho TV3]
        /// </summary>
        public void OnGroupUpdated(string groupName)
        {
            InvokeIfRequired(() => AddGroupToList(groupName));
        }

        // ================== HELPERS - SIDEBAR LIST ==================
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
                // TODO: [TV5] Thay UserId/DisplayName demo dưới đây bằng dữ liệu thật
                // ngay khi OnUserStatusChanged(...) (hoặc danh sách User ban đầu từ
                // SessionManager) cung cấp đủ thông tin.
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

        // ================== HELPERS - CHAT BUBBLE ==================
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

        /// <summary>
        /// Gọi khi người dùng bấm "Trả lời" trên 1 ChatBubble. [Hook cho TV4/TV2]
        /// </summary>
        private void ChatBubble_ReplyClicked(object? sender, ChatMessage repliedMessage)
        {
            // TODO: [TV2/TV4] Lưu repliedMessage làm ngữ cảnh Reply, hiển thị preview
            // phía trên ô nhập tin nhắn, rồi đính kèm ReplyToMessageId khi gửi tin mới.
            MessageBox.Show(
                $"Đang trả lời: \"{repliedMessage.Content}\"\n(Chức năng gửi kèm Reply sẽ hoàn thiện khi ChatService tích hợp.)",
                "Trả lời tin nhắn", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ScrollMessagesToBottom()
        {
            pnlMessages.VerticalScroll.Value = pnlMessages.VerticalScroll.Maximum;
            pnlMessages.PerformLayout();
        }

        // ================== HELPERS - KHÁC ==================
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