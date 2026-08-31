using ChatTCP.Client.Network;
using ChatTCP.Client.Services;
using ChatTCP.Client.UserControls;
using ChatTCP.Client.Utils;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Forms
{
    public partial class ClientForm : Form
    {
        // =========================================================
        // NETWORK / SERVICES
        // =========================================================

        private TcpClientManager _tcpClient = null!;
        private ChatService _chatService = null!;
        private GroupService? _groupService;
        private EmojiService _emojiService = null!;

        // =========================================================
        // CURRENT USER
        // =========================================================

        private readonly User _currentUser;
        private readonly string _currentUsername;

        private string _activeChatTarget = "";
        private bool _activeChatIsGroup = false;

        // =========================================================
        // USERS / GROUPS
        // =========================================================

        private readonly Dictionary<string, User>
            _onlineUsersByName =
                new Dictionary<string, User>();

        private int _nextDemoUserId = 1;

        // =========================================================
        // HEADER
        // =========================================================

        private Panel pnlHeader = null!;

        private PictureBox picMyAvatar = null!;
        private Label lblMyUsername = null!;
        private Label lblMyStatus = null!;
        private Button btnLogout = null!;

        // =========================================================
        // SIDEBAR
        // =========================================================

        private Panel pnlSidebar = null!;
        private TabControl tabSidebar = null!;
        private TabPage tabUsers = null!;
        private TabPage tabGroups = null!;

        private ListView lvUsers = null!;
        private ListView lvGroups = null!;

        private Button btnCreateGroup = null!;

        private ContextMenuStrip cmsUsers = null!;
        private ToolStripMenuItem miStartChat = null!;

        // =========================================================
        // CHAT
        // =========================================================

        private Panel pnlChat = null!;

        private Panel pnlChatHeader = null!;

        private PictureBox picChatAvatar = null!;
        private Label lblChatTarget = null!;
        private Label lblChatStatus = null!;

        private Panel pnlMessages = null!;
        private FlowLayoutPanel flpMessages = null!;

        private Panel pnlInput = null!;

        private Button btnEmoji = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;

        // =========================================================
        // EMOJI
        // =========================================================

        private Panel pnlEmoji = null!;
        private EmojiPickerForm? _emojiPicker;

        // =========================================================
        // ITEM REFERENCES
        // =========================================================

        private readonly Dictionary<string, ListViewItem>
            _userItems =
                new Dictionary<string, ListViewItem>();

        private readonly Dictionary<string, ListViewItem>
            _groupItems =
                new Dictionary<string, ListViewItem>();

        // =========================================================
        // AVATAR
        // =========================================================

        private Image? _defaultAvatar;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ClientForm(
            User currentUser,
            TcpClientManager tcpClient)
        {
            _currentUser = currentUser;

            _currentUsername =
                currentUser.Username;

            InitializeComponent();

            InitializeClientComponents(
                tcpClient);
        }

        // =========================================================
        // UI SETUP
        // =========================================================

        private void InitializeComponent()
        {
            Text = "ChatTCP - Client";

            Size = new Size(900, 600);

            MinimumSize =
                new Size(750, 500);

            StartPosition =
                FormStartPosition.CenterScreen;

            Font =
                new Font(
                    "Segoe UI",
                    9F);

            // =====================================================
            // LOAD AVATAR
            // =====================================================

            _defaultAvatar =
                LoadAvatar(_currentUser.Avatar)
                ?? LoadAvatar("avt1.png");

            // =====================================================
            // HEADER
            // =====================================================

            pnlHeader =
                new Panel
                {
                    Dock = DockStyle.Top,

                    Height = 58,

                    BackColor =
                        Color.FromArgb(
                            30,
                            90,
                            180),

                    Padding =
                        new Padding(
                            10,
                            0,
                            10,
                            0)
                };

            // -----------------------------------------------------
            // MY AVATAR
            // -----------------------------------------------------

            picMyAvatar =
                new PictureBox
                {
                    Size =
                        new Size(
                            38,
                            38),

                    Location =
                        new Point(
                            10,
                            10),

                    SizeMode =
                        PictureBoxSizeMode.Zoom,

                    BackColor =
                        Color.White,

                    BorderStyle =
                        BorderStyle.None
                };

            if (_defaultAvatar != null)
            {
                picMyAvatar.Image =
                    new Bitmap(
                        _defaultAvatar);
            }

            MakeCircle(
                picMyAvatar);

            // -----------------------------------------------------
            // MY USERNAME
            // -----------------------------------------------------

            lblMyUsername =
                new Label
                {
                    Text =
                        _currentUsername,

                    Location =
                        new Point(
                            58,
                            7),

                    AutoSize = true,

                    ForeColor =
                        Color.White,

                    Font =
                        new Font(
                            "Segoe UI",
                            10F,
                            FontStyle.Bold)
                };

            // -----------------------------------------------------
            // ONLINE STATUS
            // -----------------------------------------------------

            lblMyStatus =
                new Label
                {
                    Text =
                        "● Online",

                    Location =
                        new Point(
                            58,
                            29),

                    AutoSize = true,

                    ForeColor =
                        Color.FromArgb(
                            190,
                            255,
                            205),

                    Font =
                        new Font(
                            "Segoe UI",
                            8F)
                };

            // -----------------------------------------------------
            // LOGOUT BUTTON
            // -----------------------------------------------------

            btnLogout =
                new Button
                {
                    Text =
                        "Đăng Xuất",

                    Width =
                        110,

                    Height =
                        34,

                    Location =
                        new Point(
                            pnlHeader.Width - 120,
                            12),

                    Anchor =
                        AnchorStyles.Top |
                        AnchorStyles.Right,

                    FlatStyle =
                        FlatStyle.Flat,

                    BackColor =
                        Color.White,

                    ForeColor =
                        Color.FromArgb(
                            30,
                            90,
                            180),

                    Font =
                        new Font(
                            "Segoe UI",
                            9F,
                            FontStyle.Bold),

                    Cursor =
                        Cursors.Hand,

                    TabStop =
                        false
                };

            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click +=
                BtnLogout_Click;

            pnlHeader.Controls.AddRange(
                new Control[]
                {
                    picMyAvatar,
                    lblMyUsername,
                    lblMyStatus,
                    btnLogout
                });

            // =====================================================
            // CONTEXT MENU
            // =====================================================

            cmsUsers =
                new ContextMenuStrip();

            miStartChat =
                new ToolStripMenuItem(
                    "Nhắn tin");

            miStartChat.Click +=
                MiStartChat_Click;

            cmsUsers.Items.Add(
                miStartChat);

            // =====================================================
            // SIDEBAR
            // =====================================================

            pnlSidebar =
                new Panel
                {
                    Dock = DockStyle.Left,

                    Width = 220,

                    BackColor =
                        Color.White
                };

            tabSidebar =
                new TabControl
                {
                    Dock =
                        DockStyle.Fill
                };

            // =====================================================
            // USERS TAB
            // =====================================================

            tabUsers =
                new TabPage(
                    "Users");

            lvUsers =
                new ListView
                {
                    Dock =
                        DockStyle.Fill,

                    View =
                        View.Details,

                    FullRowSelect =
                        true,

                    GridLines =
                        false,

                    HeaderStyle =
                        ColumnHeaderStyle.None,

                    ContextMenuStrip =
                        cmsUsers
                };

            lvUsers.Columns.Add(
                "User",
                216);

            lvUsers.MouseDown +=
                LvUsers_MouseDown;

            lvUsers.DoubleClick +=
                (s, e) =>
                    OpenChatWithUser();

            tabUsers.Controls.Add(
                lvUsers);

            // =====================================================
            // GROUPS TAB
            // =====================================================

            tabGroups =
                new TabPage(
                    "Groups");

            lvGroups =
                new ListView
                {
                    Dock =
                        DockStyle.Fill,

                    View =
                        View.Details,

                    FullRowSelect =
                        true,

                    GridLines =
                        false,

                    HeaderStyle =
                        ColumnHeaderStyle.None
                };

            lvGroups.Columns.Add(
                "Group",
                216);

            lvGroups.DoubleClick +=
                (s, e) =>
                    OpenChatWithGroup();

            tabGroups.Controls.Add(
                lvGroups);

            tabSidebar.TabPages.Add(
                tabUsers);

            tabSidebar.TabPages.Add(
                tabGroups);

            // =====================================================
            // CREATE GROUP BUTTON
            // =====================================================

            btnCreateGroup =
                new Button
                {
                    Text =
                        "+ Tạo nhóm mới",

                    Dock =
                        DockStyle.Bottom,

                    Height =
                        34,

                    FlatStyle =
                        FlatStyle.Flat,

                    BackColor =
                        Color.White
                };

            btnCreateGroup.Click +=
                BtnCreateGroup_Click;

            pnlSidebar.Controls.Add(
                tabSidebar);

            pnlSidebar.Controls.Add(
                btnCreateGroup);

            // =====================================================
            // CHAT PANEL
            // =====================================================

            pnlChat =
                new Panel
                {
                    Dock =
                        DockStyle.Fill,

                    BackColor =
                        Color.White
                };

            // =====================================================
            // CHAT HEADER
            // =====================================================

            pnlChatHeader =
                new Panel
                {
                    Dock =
                        DockStyle.Top,

                    Height =
                        58,

                    BackColor =
                        Color.WhiteSmoke,

                    Padding =
                        new Padding(
                            10,
                            0,
                            10,
                            0)
                };

            // -----------------------------------------------------
            // CHAT AVATAR
            // -----------------------------------------------------

            picChatAvatar =
                new PictureBox
                {
                    Size =
                        new Size(
                            38,
                            38),

                    Location =
                        new Point(
                            10,
                            10),

                    SizeMode =
                        PictureBoxSizeMode.Zoom,

                    BackColor =
                        Color.LightGray,

                    BorderStyle =
                        BorderStyle.None
                };

            if (_defaultAvatar != null)
            {
                picChatAvatar.Image =
                    new Bitmap(
                        _defaultAvatar);
            }

            MakeCircle(
                picChatAvatar);

            // -----------------------------------------------------
            // CHAT TARGET
            // -----------------------------------------------------

            lblChatTarget =
                new Label
                {
                    Text =
                        "Chọn một cuộc trò chuyện",

                    Location =
                        new Point(
                            64,
                            6),

                    AutoSize = true,

                    MaximumSize =
                        new Size(
                            500,
                            22),

                    Font =
                        new Font(
                            "Segoe UI",
                            10.5F,
                            FontStyle.Bold),

                    ForeColor =
                        Color.FromArgb(
                            35,
                            35,
                            35)
                };

            // -----------------------------------------------------
            // CHAT STATUS
            // -----------------------------------------------------

            lblChatStatus =
                new Label
                {
                    Text =
                        "",

                    Location =
                        new Point(
                            64,
                            30),

                    AutoSize = true,

                    ForeColor =
                        Color.Gray,

                    Font =
                        new Font(
                            "Segoe UI",
                            8F)
                };

            pnlChatHeader.Controls.AddRange(
                new Control[]
                {
                    picChatAvatar,
                    lblChatTarget,
                    lblChatStatus
                });

            // =====================================================
            // MESSAGE PANEL
            // =====================================================

            pnlMessages =
                new Panel
                {
                    Dock =
                        DockStyle.Fill,

                    BackColor =
                        Color.White,

                    AutoScroll =
                        true,

                    Padding =
                        new Padding(
                            0)
                };

            flpMessages =
                new FlowLayoutPanel
                {
                    Dock =
                        DockStyle.Top,

                    FlowDirection =
                        FlowDirection.TopDown,

                    WrapContents =
                        false,

                    AutoSize =
                        true,

                    AutoSizeMode =
                        AutoSizeMode.GrowAndShrink,

                    Padding =
                        new Padding(
                            10),

                    BackColor =
                        Color.White
                };

            flpMessages.Resize +=
                FlpMessages_Resize;

            pnlMessages.Controls.Add(
                flpMessages);

            // =====================================================
            // INPUT PANEL
            // =====================================================

            pnlInput =
                new Panel
                {
                    Dock =
                        DockStyle.Bottom,

                    Height =
                        48,

                    Padding =
                        new Padding(
                            6),

                    BackColor =
                        Color.White
                };

            // -----------------------------------------------------
            // EMOJI BUTTON
            // -----------------------------------------------------

            btnEmoji =
                new Button
                {
                    Text =
                        "😊",

                    Location =
                        new Point(
                            6,
                            7),

                    Width =
                        34,

                    Height =
                        34,

                    FlatStyle =
                        FlatStyle.Flat,

                    Padding =
                        new Padding(0),

                    Margin =
                        new Padding(0),

                    Font =
                        new Font(
                            "Segoe UI Emoji",
                            11F),

                    UseVisualStyleBackColor =
                        false,

                    BackColor =
                        Color.White,

                    Cursor =
                        Cursors.Hand
                };

            btnEmoji.FlatAppearance.BorderSize = 1;

            btnEmoji.Click +=
                BtnEmoji_Click;

            // -----------------------------------------------------
            // SEND BUTTON
            // -----------------------------------------------------

            btnSend =
                new Button
                {
                    Text =
                        "Gửi",

                    Width =
                        70,

                    Height =
                        34,

                    FlatStyle =
                        FlatStyle.Flat,

                    BackColor =
                        Color.FromArgb(
                            30,
                            90,
                            180),

                    ForeColor =
                        Color.White
                };

            btnSend.Click +=
                BtnSend_Click;

            // -----------------------------------------------------
            // MESSAGE BOX
            // -----------------------------------------------------

            txtMessage =
                new TextBox
                {
                    Location =
                        new Point(
                            48,
                            7),

                    Height =
                        34,

                    Font =
                        new Font(
                            "Segoe UI",
                            10F),

                    BorderStyle =
                        BorderStyle.FixedSingle,

                    PlaceholderText =
                        "Nhập tin nhắn..."
                };

            txtMessage.KeyDown +=
                TxtMessage_KeyDown;

            pnlInput.Controls.AddRange(
                new Control[]
                {
                    btnEmoji,
                    txtMessage,
                    btnSend
                });

            pnlInput.Resize +=
                (s, e) =>
                    LayoutInputBar();

            // =====================================================
            // EMOJI PANEL
            // =====================================================

            pnlEmoji =
                new Panel
                {
                    Dock =
                        DockStyle.Bottom,

                    Height =
                        165,

                    BackColor =
                        Color.White,

                    Visible =
                        false,

                    BorderStyle =
                        BorderStyle.FixedSingle
                };

            // =====================================================
            // ADD CONTROLS
            // =====================================================

            pnlChat.Controls.Add(
                pnlMessages);

            pnlChat.Controls.Add(
                pnlEmoji);

            pnlChat.Controls.Add(
                pnlInput);

            pnlChat.Controls.Add(
                pnlChatHeader);

            Controls.Add(
                pnlChat);

            Controls.Add(
                pnlSidebar);

            Controls.Add(
                pnlHeader);

            // =====================================================
            // EVENTS
            // =====================================================

            Load +=
                (s, e) =>
                {
                    LayoutInputBar();
                    ResizeMessageRows();
                };

            FormClosing +=
                ClientForm_FormClosing;
        }

        // =========================================================
        // CLIENT SETUP
        // =========================================================

        private void InitializeClientComponents(
            TcpClientManager tcpClient)
        {
            // -----------------------------------------------------
            // SESSION
            // -----------------------------------------------------

            SessionManager.Instance.SetCurrentUser(
                _currentUser);

            SessionManager.Instance.HistoryUpdated +=
                OnHistoryUpdated;

            _tcpClient = tcpClient;

            // -----------------------------------------------------
            // CHAT SERVICE
            // -----------------------------------------------------

            _chatService =
                new ChatService(
                    _tcpClient);

            _chatService.OnMessageReceived +=
                ChatService_OnMessageReceived;

            // -----------------------------------------------------
            // GROUP SERVICE
            // -----------------------------------------------------

            _groupService =
                new GroupService(
                    _tcpClient,
                    _currentUser.UserId);

            // -----------------------------------------------------
            // EMOJI SERVICE
            // -----------------------------------------------------

            _emojiService =
                new EmojiService();

            // -----------------------------------------------------
            // LOAD ONLINE USERS
            // -----------------------------------------------------

            var onlineUsers =
                SessionManager.Instance
                    .GetOnlineUsers();

            foreach (var u in onlineUsers)
            {
                if (u.Username ==
                    _currentUsername)
                {
                    continue;
                }

                AddUserToList(
                    u.Username,
                    string.Equals(
                        u.Status,
                        "Online",
                        StringComparison.OrdinalIgnoreCase));
            }

            // -----------------------------------------------------
            // DEMO USERS
            // -----------------------------------------------------

            AddUserToList(
                "an_nguyen",
                true);

            AddUserToList(
                "minh_le",
                true);

            AddUserToList(
                "thu_tran",
                false);

            // -----------------------------------------------------
            // DEMO GROUP
            // -----------------------------------------------------

            AddGroupToList(
                "Nhóm Đồ Án UDM08");
        }

        // =========================================================
        // USER
        // =========================================================

        private void LvUsers_MouseDown(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button ==
                MouseButtons.Right)
            {
                var item =
                    lvUsers.GetItemAt(
                        e.X,
                        e.Y);

                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }

        private void MiStartChat_Click(
            object? sender,
            EventArgs e)
        {
            OpenChatWithUser();
        }

        private void OpenChatWithUser()
        {
            if (lvUsers.SelectedItems.Count == 0)
            {
                return;
            }

            string username =
                lvUsers.SelectedItems[0]
                    .Tag as string
                ??
                lvUsers.SelectedItems[0].Text;

            _activeChatTarget =
                username;

            _activeChatIsGroup =
                false;

            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            lblChatTarget.Text =
                username;

            lblChatStatus.Text =
                "Online";

            if (_defaultAvatar != null)
            {
                SetPictureBoxImage(
                    picChatAvatar,
                    _defaultAvatar);
            }

            // -----------------------------------------------------
            // SESSION
            // -----------------------------------------------------

            SessionManager.Instance
                .SetActiveDirectChat(
                    username,
                    tcpClient:
                        _tcpClient);

            // -----------------------------------------------------
            // LOAD HISTORY
            // -----------------------------------------------------

            flpMessages.Controls.Clear();

            var cachedHistory =
                SessionManager.Instance
                    .GetDirectChatHistory(
                        username);

            foreach (var msg in cachedHistory)
            {
                AddMessageBubble(
                    msg,
                    msg.SenderName ==
                    _currentUsername);
            }

            ScrollMessagesToBottom();
        }

        // =========================================================
        // GROUP CHAT
        // =========================================================

        private void OpenChatWithGroup()
        {
            if (lvGroups.SelectedItems.Count == 0)
            {
                return;
            }

            string groupName =
                lvGroups.SelectedItems[0].Text;

            _activeChatTarget =
                groupName;

            _activeChatIsGroup =
                true;

            lblChatTarget.Text =
                groupName;

            lblChatStatus.Text =
                "Nhóm chat";

            if (_defaultAvatar != null)
            {
                SetPictureBoxImage(
                    picChatAvatar,
                    _defaultAvatar);
            }

            SessionManager.Instance
                .SetActiveGroupChat(
                    groupName,
                    groupId: null,
                    tcpClient:
                        _tcpClient);

            flpMessages.Controls.Clear();

            var cachedHistory =
                SessionManager.Instance
                    .GetGroupChatHistory(
                        groupName);

            foreach (var msg in cachedHistory)
            {
                AddMessageBubble(
                    msg,
                    msg.SenderName ==
                    _currentUsername);
            }

            ScrollMessagesToBottom();
        }

        // =========================================================
        // CREATE GROUP
        // =========================================================

        private void BtnCreateGroup_Click(
            object? sender,
            EventArgs e)
        {
            if (_groupService == null)
            {
                MessageBox.Show(
                    "GroupService chưa được khởi tạo.",
                    "Tạo nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (_tcpClient == null ||
                !_tcpClient.IsConnected)
            {
                MessageBox.Show(
                    "Client chưa kết nối đến Server.",
                    "Không thể tạo nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            using (
                var createGroupForm =
                    new CreateGroupForm(
                        _groupService,
                        _onlineUsersByName.Values))
            {
                if (
                    createGroupForm.ShowDialog(this)
                    ==
                    DialogResult.OK
                    &&
                    createGroupForm.CreatedGroup != null)
                {
                    AddGroupToList(
                        createGroupForm
                            .CreatedGroup
                            .GroupName);
                }
            }
        }

        // =========================================================
        // SEND MESSAGE
        // =========================================================

        private void TxtMessage_KeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter &&
                !e.Shift)
            {
                e.SuppressKeyPress = true;

                SendCurrentMessage();
            }
        }

        private void BtnSend_Click(
            object? sender,
            EventArgs e)
        {
            SendCurrentMessage();
        }

        private void SendCurrentMessage()
        {
            string content =
                txtMessage.Text.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            if (string.IsNullOrEmpty(
                _activeChatTarget))
            {
                MessageBox.Show(
                    "Vui lòng chọn một người dùng " +
                    "hoặc nhóm để chat.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (_tcpClient == null ||
                !_tcpClient.IsConnected)
            {
                MessageBox.Show(
                    "Client chưa kết nối đến Server.",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            var message =
                new ChatMessage
                {
                    SenderId =
                        _currentUser.UserId,

                    SenderName =
                        _currentUsername,

                    Content =
                        content,

                    Timestamp =
                        DateTime.Now
                };

            // =====================================================
            // GROUP
            // =====================================================

            if (_activeChatIsGroup)
            {
                message.Type =
                    MessageType.GroupChat;

                int? groupId =
                    SessionManager.Instance
                        .ActiveGroupId;

                if (
                    groupId.HasValue &&
                    groupId.Value > 0 &&
                    _groupService != null)
                {
                    message.GroupId =
                        groupId.Value;

                    _groupService.SendGroupMessage(
                        groupId.Value,
                        content);
                }
                else
                {
                    _chatService.SendMessage(
                        message);
                }
            }
            else
            {
                // =================================================
                // DIRECT CHAT
                // =================================================

                message.Type =
                    MessageType.DirectChat;

                if (
                    _onlineUsersByName.TryGetValue(
                        _activeChatTarget,
                        out var targetUser))
                {
                    message.ReceiverId =
                        targetUser.UserId;
                }

                _chatService.SendMessage(
                    message);
            }

            // =====================================================
            // SESSION
            // =====================================================

            SessionManager.Instance
                .AddMessage(
                    message);

            // =====================================================
            // ADD TO UI
            // =====================================================

            AddMessageBubble(
                message,
                isMine: true);

            txtMessage.Clear();

            txtMessage.Focus();
        }

        // =========================================================
        // EMOJI
        // =========================================================

        private void BtnEmoji_Click(
            object? sender,
            EventArgs e)
        {
            ToggleEmojiPicker();
        }

        private void ToggleEmojiPicker()
        {
            if (pnlEmoji.Visible)
            {
                CloseEmojiPicker();
                return;
            }

            try
            {
                _emojiPicker =
                    new EmojiPickerForm();

                _emojiPicker.TopLevel =
                    false;

                _emojiPicker.FormBorderStyle =
                    FormBorderStyle.None;

                _emojiPicker.Dock =
                    DockStyle.Fill;

                _emojiPicker.ShowInTaskbar =
                    false;

                _emojiPicker.FormClosed +=
                    EmojiPicker_FormClosed;

                pnlEmoji.Controls.Clear();

                pnlEmoji.Controls.Add(
                    _emojiPicker);

                pnlEmoji.Visible =
                    true;

                _emojiPicker.Show();

                _emojiPicker.BringToFront();

                pnlInput.BringToFront();
            }
            catch
            {
                pnlEmoji.Visible = false;

                using (
                    var picker =
                        new EmojiPickerForm())
                {
                    picker.StartPosition =
                        FormStartPosition.Manual;

                    Point screenPoint =
                        btnEmoji.PointToScreen(
                            new Point(
                                0,
                                -picker.Height - 5));

                    picker.Location =
                        screenPoint;

                    if (
                        picker.ShowDialog(this)
                        ==
                        DialogResult.OK
                        &&
                        !string.IsNullOrEmpty(
                            picker.SelectedEmoji))
                    {
                        InsertEmoji(
                            picker.SelectedEmoji);
                    }
                }
            }
        }

        private void EmojiPicker_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            if (_emojiPicker != null)
            {
                string emoji =
                    _emojiPicker.SelectedEmoji;

                if (!string.IsNullOrEmpty(
                    emoji))
                {
                    InsertEmoji(
                        emoji);
                }
            }

            pnlEmoji.Visible =
                false;

            pnlEmoji.Controls.Clear();

            _emojiPicker =
                null;

            LayoutInputBar();

            txtMessage.Focus();
        }

        private void InsertEmoji(
            string emoji)
        {
            if (string.IsNullOrEmpty(
                emoji))
            {
                return;
            }

            int position =
                txtMessage.SelectionStart;

            if (position < 0 ||
                position > txtMessage.Text.Length)
            {
                position =
                    txtMessage.Text.Length;
            }

            txtMessage.Text =
                txtMessage.Text.Insert(
                    position,
                    emoji);

            txtMessage.SelectionStart =
                position +
                emoji.Length;

            txtMessage.Focus();
        }

        private void CloseEmojiPicker()
        {
            if (_emojiPicker != null)
            {
                try
                {
                    _emojiPicker.Close();
                }
                catch
                {
                }
            }

            pnlEmoji.Visible =
                false;

            pnlEmoji.Controls.Clear();

            _emojiPicker =
                null;
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        private void BtnLogout_Click(
            object? sender,
            EventArgs e)
        {
            var result =
                MessageBox.Show(
                    "Bạn có chắc muốn đăng xuất không?",
                    "Đăng xuất",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            // Đóng ClientForm sẽ chạy ClientForm_FormClosing,
            // tại đó TCP connection và các service được giải phóng.
            Close();
        }

        // =========================================================
        // FORM CLOSING
        // =========================================================

        private void ClientForm_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            try
            {
                CloseEmojiPicker();

                SessionManager.Instance
                    .HistoryUpdated -=
                    OnHistoryUpdated;

                if (_chatService != null)
                {
                    _chatService.OnMessageReceived -=
                        ChatService_OnMessageReceived;

                    _chatService.Dispose();
                }

                _groupService?.Dispose();

                _tcpClient?.Disconnect();
            }
            catch
            {
            }

            Application.Exit();
        }

        // =========================================================
        // HISTORY
        // =========================================================

        private void OnHistoryUpdated(
            string conversationKey,
            IReadOnlyList<ChatMessage> messages)
        {
            InvokeIfRequired(() =>
            {
                if (
                    conversationKey ==
                    SessionManager.Instance
                        .GetActiveConversationKey())
                {
                    flpMessages.Controls.Clear();

                    foreach (var msg in messages)
                    {
                        AddMessageBubble(
                            msg,
                            msg.SenderName ==
                            _currentUsername);
                    }

                    ScrollMessagesToBottom();
                }
            });
        }

        // =========================================================
        // RECEIVED MESSAGE
        // =========================================================

        private void ChatService_OnMessageReceived(
            ChatMessage message)
        {
            OnMessageReceived(
                message,
                isGroupMessage: false);
        }

        public void OnMessageReceived(
            ChatMessage message,
            bool isGroupMessage)
        {
            InvokeIfRequired(() =>
            {
                SessionManager.Instance
                    .AddMessage(
                        message);

                bool isCurrentChat =
                    (
                        message.SenderName ==
                        _activeChatTarget
                    )
                    &&
                    (
                        isGroupMessage ==
                        _activeChatIsGroup
                    );

                if (isCurrentChat)
                {
                    AddMessageBubble(
                        message,
                        isMine: false);
                }
            });
        }

        // =========================================================
        // USER STATUS
        // =========================================================

        public void OnUserStatusChanged(
            string username,
            bool isOnline)
        {
            InvokeIfRequired(() =>
            {
                SessionManager.Instance
                    .UpdateUserStatus(
                        username,
                        isOnline);

                if (
                    _userItems.TryGetValue(
                        username,
                        out var item))
                {
                    UpdateUserStatus(
                        item,
                        isOnline);
                }
                else
                {
                    AddUserToList(
                        username,
                        isOnline);
                }
            });
        }

        // =========================================================
        // GROUP UPDATE
        // =========================================================

        public void OnGroupUpdated(
            string groupName)
        {
            InvokeIfRequired(
                () =>
                    AddGroupToList(
                        groupName));
        }

        // =========================================================
        // ADD USER
        // =========================================================

        private void AddUserToList(
            string username,
            bool isOnline)
        {
            if (string.Equals(
                username,
                _currentUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_userItems.ContainsKey(
                username))
            {
                UpdateUserStatus(
                    _userItems[username],
                    isOnline);

                return;
            }

            var item =
                new ListViewItem(
                    username)
                {
                    Tag =
                        username
                };

            item.ForeColor =
                isOnline
                    ? Color.Black
                    : Color.Gray;

            lvUsers.Items.Add(
                item);

            _userItems[username] =
                item;

            if (
                !_onlineUsersByName.ContainsKey(
                    username))
            {
                _onlineUsersByName[username] =
                    new User
                    {
                        UserId =
                            _nextDemoUserId++,

                        Username =
                            username,

                        DisplayName =
                            username
                    };
            }
        }

        private void UpdateUserStatus(
            ListViewItem item,
            bool isOnline)
        {
            item.ForeColor =
                isOnline
                    ? Color.Black
                    : Color.Gray;
        }

        // =========================================================
        // ADD GROUP
        // =========================================================

        private void AddGroupToList(
            string groupName)
        {
            if (
                _groupItems.ContainsKey(
                    groupName))
            {
                return;
            }

            var item =
                new ListViewItem(
                    groupName);

            lvGroups.Items.Add(
                item);

            _groupItems[groupName] =
                item;
        }

        // =========================================================
        // CHAT BUBBLE
        // =========================================================

        private void AddMessageBubble(
            ChatMessage message,
            bool isMine)
        {
            var bubble =
                new ChatBubble();

            bubble.SetMessage(
                message,
                isMine);

            bubble.ReplyClicked +=
                ChatBubble_ReplyClicked;

            int rowWidth =
                Math.Max(
                    100,
                    flpMessages.ClientSize.Width
                    -
                    flpMessages.Padding.Left
                    -
                    flpMessages.Padding.Right
                    -
                    SystemInformation.VerticalScrollBarWidth);

            var row =
                new Panel
                {
                    Width =
                        rowWidth,

                    Height =
                        Math.Max(
                            bubble.Height + 8,
                            45),

                    Margin =
                        new Padding(
                            0,
                            2,
                            0,
                            2),

                    Padding =
                        new Padding(
                            0),

                    BackColor =
                        Color.Transparent
                };

            bubble.AutoSize =
                true;

            bubble.MaximumSize =
                new Size(
                    Math.Max(
                        150,
                        rowWidth * 65 / 100),
                    0);

            row.Controls.Add(
                bubble);

            bubble.PerformLayout();

            row.Height =
                Math.Max(
                    bubble.Height + 8,
                    45);

            if (isMine)
            {
                bubble.Location =
                    new Point(
                        Math.Max(
                            0,
                            row.Width -
                            bubble.Width -
                            5),
                        4);
            }
            else
            {
                bubble.Location =
                    new Point(
                        5,
                        4);
            }

            flpMessages.Controls.Add(
                row);

            ScrollMessagesToBottom();
        }

        // =========================================================
        // RESIZE MESSAGE ROWS
        // =========================================================

        private void FlpMessages_Resize(
            object? sender,
            EventArgs e)
        {
            ResizeMessageRows();
        }

        private void ResizeMessageRows()
        {
            if (flpMessages == null)
            {
                return;
            }

            int rowWidth =
                Math.Max(
                    100,
                    flpMessages.ClientSize.Width
                    -
                    flpMessages.Padding.Left
                    -
                    flpMessages.Padding.Right
                    -
                    SystemInformation.VerticalScrollBarWidth);

            foreach (Control control
                     in flpMessages.Controls)
            {
                if (control is not Panel row)
                {
                    continue;
                }

                row.Width =
                    rowWidth;

                if (row.Controls.Count == 0)
                {
                    continue;
                }

                Control bubble =
                    row.Controls[0];

                bool isMine =
                    bubble.Tag as string ==
                    "MINE";

                if (bubble.Tag == null)
                {
                    isMine =
                        bubble.Left >
                        row.Width / 2;
                }

                if (isMine)
                {
                    bubble.Left =
                        Math.Max(
                            0,
                            row.Width -
                            bubble.Width -
                            5);
                }
                else
                {
                    bubble.Left =
                        5;
                }
            }
        }

        // =========================================================
        // REPLY
        // =========================================================

        private void ChatBubble_ReplyClicked(
            object? sender,
            ChatMessage repliedMessage)
        {
            MessageBox.Show(
                $"Đang trả lời: \"{repliedMessage.Content}\"\n" +
                "(Chức năng gửi kèm Reply sẽ hoàn thiện " +
                "khi ChatService tích hợp.)",
                "Trả lời tin nhắn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // =========================================================
        // SCROLL
        // =========================================================

        private void ScrollMessagesToBottom()
        {
            if (pnlMessages == null)
            {
                return;
            }

            pnlMessages.PerformLayout();

            try
            {
                pnlMessages.VerticalScroll.Value =
                    pnlMessages.VerticalScroll.Maximum;
            }
            catch
            {
            }

            pnlMessages.AutoScrollPosition =
                new Point(
                    0,
                    pnlMessages.VerticalScroll.Maximum);

            pnlMessages.PerformLayout();
        }

        // =========================================================
        // INPUT LAYOUT
        // =========================================================

        private void LayoutInputBar()
        {
            if (pnlInput == null ||
                btnSend == null ||
                txtMessage == null ||
                btnEmoji == null)
            {
                return;
            }

            btnSend.Location =
                new Point(
                    pnlInput.Width -
                    btnSend.Width -
                    6,
                    7);

            txtMessage.Location =
                new Point(
                    btnEmoji.Right + 8,
                    7);

            txtMessage.Width =
                Math.Max(
                    100,
                    btnSend.Left -
                    txtMessage.Left -
                    8);
        }

        // =========================================================
        // AVATAR LOADER
        // =========================================================

        private static Image? LoadAvatar(
            string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            try
            {
                string path =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "Resources",
                        "Avatars",
                        fileName);

                if (!File.Exists(path))
                {
                    return null;
                }

                // Clone để file ảnh không bị lock
                using var temp =
                    Image.FromFile(path);

                return new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }

        private static void SetPictureBoxImage(
            PictureBox pictureBox,
            Image image)
        {
            try
            {
                var oldImage =
                    pictureBox.Image;

                pictureBox.Image =
                    new Bitmap(image);

                if (oldImage != null)
                {
                    oldImage.Dispose();
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // CIRCLE AVATAR
        // =========================================================

        private static void MakeCircle(
            Control control)
        {
            using var path =
                new GraphicsPath();

            path.AddEllipse(
                0,
                0,
                control.Width - 1,
                control.Height - 1);

            control.Region =
                new Region(path);
        }

        // =========================================================
        // INITIALS
        // =========================================================

        private static string GetInitials(
            string username)
        {
            if (string.IsNullOrEmpty(
                username))
            {
                return "?";
            }

            return username.Length >= 2
                ? username
                    .Substring(0, 2)
                    .ToUpper()
                : username
                    .Substring(0, 1)
                    .ToUpper();
        }

        // =========================================================
        // UI THREAD
        // =========================================================

        private void InvokeIfRequired(
            Action action)
        {
            if (IsDisposed ||
                Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    Invoke(action);
                }
                catch
                {
                }
            }
            else
            {
                action();
            }
        }
    }
}
