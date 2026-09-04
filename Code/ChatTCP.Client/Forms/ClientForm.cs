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
        // NETWORK / SERVICES
        private TcpClientManager _tcpClient = null!;
        private ChatService _chatService = null!;
        private GroupService? _groupService;
        private EmojiService _emojiService = null!;
        // CURRENT USER
        private readonly User _currentUser;
        private readonly string _currentUsername;
        public bool IsLoggingOut { get; private set; } = false;
        private string _activeChatTarget = "";
        private bool _activeChatIsGroup = false;
        // USERS / GROUPS
        private readonly Dictionary<string, User>
            _onlineUsersByName =
                new Dictionary<string, User>();
        private readonly Dictionary<string, Group>
            _groupsByName =
                new Dictionary<string, Group>();
        private ChatMessage? _activeReplyMessage;
        private int _nextDemoUserId = 1;
        // HEADER
        private Panel pnlHeader = null!;
        private PictureBox picMyAvatar = null!;
        private Label lblMyUsername = null!;
        private Label lblMyStatus = null!;
        private Button btnLogout = null!;
        // SIDEBAR
        private Panel pnlSidebar = null!;
        private TabControl tabSidebar = null!;
        private TabPage tabUsers = null!;
        private TabPage tabGroups = null!;
        private ListView lvUsers = null!;
        private ListView lvGroups = null!;
        private Button btnCreateGroup = null!;
        private ContextMenuStrip cmsUsers = null!;
        private ToolStripMenuItem miStartChat = null!;
        // CHAT
        private Panel pnlChat = null!;
        private Panel pnlChatHeader = null!;
        private PictureBox picChatAvatar = null!;
        private Label lblChatTarget = null!;
        private Label lblChatStatus = null!;
        private Panel pnlMessages = null!;
        private FlowLayoutPanel flpMessages = null!;
        private Panel pnlReplyPreview = null!;
        private Label lblReplyPreview = null!;
        private Button btnCancelReply = null!;
        private Panel pnlInput = null!;
        private Button btnEmoji = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;
        // EMOJI
        private Panel pnlEmoji = null!;
        private EmojiPickerForm? _emojiPicker;
        // ITEM REFERENCES
        private readonly Dictionary<string, ListViewItem>
            _userItems =
                new Dictionary<string, ListViewItem>();
        private readonly Dictionary<string, ListViewItem>
            _groupItems =
                new Dictionary<string, ListViewItem>();
        // AVATAR & CHAT STATE
        private Image? _myAvatar;
        private DateTime? _lastRenderedDate = null;
        // CONSTRUCTOR
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
        // UI SETUP
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
            // LOAD AVATAR
            _myAvatar =
                LoadAvatar(_currentUser.Avatar)
                ?? LoadAvatar("avt1.png");
            // HEADER
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
            // MY AVATAR
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
            if (_myAvatar != null)
            {
                picMyAvatar.Image =
                    new Bitmap(
                        _myAvatar);
            }
            MakeCircle(
                picMyAvatar);
            // MY USERNAME
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
            // ONLINE STATUS
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
            // LOGOUT BUTTON
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
            // CONTEXT MENU
            cmsUsers =
                new ContextMenuStrip();
            miStartChat =
                new ToolStripMenuItem(
                    "Nhắn tin");
            miStartChat.Click +=
                MiStartChat_Click;
            cmsUsers.Items.Add(
                miStartChat);
            // SIDEBAR
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
            // USERS TAB
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
            lvUsers.Click +=
                (s, e) =>
                    OpenChatWithUser();
            lvUsers.DoubleClick +=
                (s, e) =>
                    OpenChatWithUser();
            tabUsers.Controls.Add(
                lvUsers);
            // GROUPS TAB
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
            lvGroups.Click +=
                (s, e) =>
                    OpenChatWithGroup();
            lvGroups.DoubleClick +=
                (s, e) =>
                    OpenChatWithGroup();
            tabGroups.Controls.Add(
                lvGroups);
            tabSidebar.TabPages.Add(
                tabUsers);
            tabSidebar.TabPages.Add(
                tabGroups);
            // CREATE GROUP BUTTON
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
            // CHAT PANEL
            pnlChat =
                new Panel
                {
                    Dock =
                        DockStyle.Fill,
                    BackColor =
                        Color.White
                };
            // CHAT HEADER
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
            // CHAT AVATAR
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
            var initialChatAvatar = LoadAvatar("avt1.png");
            if (initialChatAvatar != null)
            {
                picChatAvatar.Image =
                    new Bitmap(
                        initialChatAvatar);
            }
            MakeCircle(
                picChatAvatar);
            // CHAT TARGET
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
            // CHAT STATUS
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
            // MESSAGE PANEL
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
            // INPUT PANEL
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
            // EMOJI BUTTON
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
            // SEND BUTTON
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
            // MESSAGE BOX
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
            // REPLY PREVIEW PANEL
            pnlReplyPreview =
                new Panel
                {
                    Dock =
                        DockStyle.Bottom,
                    Height =
                        32,
                    BackColor =
                        Color.FromArgb(
                            245,
                            247,
                            250),
                    Padding =
                        new Padding(
                            10,
                            4,
                            10,
                            4),
                    Visible =
                        false
                };
            lblReplyPreview =
                new Label
                {
                    Location =
                        new Point(
                            10,
                            6),
                    AutoSize =
                        true,
                    Font =
                        new Font(
                            "Segoe UI",
                            9F,
                            FontStyle.Italic),
                    ForeColor =
                        Color.FromArgb(
                            70,
                            70,
                            70),
                    Text =
                        "↩ Trả lời:"
                };
            btnCancelReply =
                new Button
                {
                    Text =
                        "✕",
                    Size =
                        new Size(
                            24,
                            24),
                    Anchor =
                        AnchorStyles.Top |
                        AnchorStyles.Right,
                    FlatStyle =
                        FlatStyle.Flat,
                    ForeColor =
                        Color.DimGray,
                    Cursor =
                        Cursors.Hand,
                    Font =
                        new Font(
                            "Segoe UI",
                            8.5F,
                            FontStyle.Bold)
                };
            btnCancelReply.FlatAppearance.BorderSize = 0;
            btnCancelReply.Click +=
                BtnCancelReply_Click;
            pnlReplyPreview.Controls.Add(
                lblReplyPreview);
            pnlReplyPreview.Controls.Add(
                btnCancelReply);
            pnlReplyPreview.Resize +=
                (s, e) =>
                {
                    btnCancelReply.Location =
                        new Point(
                            pnlReplyPreview.Width -
                            btnCancelReply.Width -
                            8,
                            4);
                    lblReplyPreview.MaximumSize =
                        new Size(
                            Math.Max(
                                50,
                                btnCancelReply.Left -
                                lblReplyPreview.Left -
                                10),
                            24);
                };
            // EMOJI PANEL
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
            // ADD CONTROLS
            pnlChat.Controls.Add(
                pnlMessages);
            pnlChat.Controls.Add(
                pnlEmoji);
            pnlChat.Controls.Add(
                pnlReplyPreview);
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
            // EVENTS
            Load +=
                (s, e) =>
                {
                    LayoutInputBar();
                    ResizeMessageRows();
                };
            FormClosing +=
                ClientForm_FormClosing;
        }
        // CLIENT SETUP
        private void InitializeClientComponents(
            TcpClientManager tcpClient)
        {
            // SESSION
            SessionManager.Instance.SetCurrentUser(
                _currentUser);
            SessionManager.Instance.HistoryUpdated +=
                OnHistoryUpdated;
            _tcpClient = tcpClient;
            // CHAT SERVICE
            _chatService =
                new ChatService(
                    _tcpClient);
            _chatService.OnMessageReceived +=
                ChatService_OnMessageReceived;
            // GROUP SERVICE
            _groupService =
                new GroupService(
                    _tcpClient,
                    _currentUser.UserId);
            _groupService.GroupListReceived +=
                OnGroupListReceived;
            _groupService.GroupCreated +=
                OnGroupCreated;
            _groupService.GroupMessageReceived +=
                OnGroupMessageReceived;
            _groupService.RequestGroupList();
            // EMOJI SERVICE
            _emojiService =
                new EmojiService();
            // LISTEN TO TCP MESSAGES (UserList & UserStatus)
            _tcpClient.MessageReceived +=
                OnTcpClientMessageReceived;
            // REQUEST USER LIST FROM DATABASE
            _tcpClient.SendMessage(new ChatMessage
            {
                SenderId = _currentUser.UserId,
                SenderName = _currentUser.Username,
                Type = MessageType.GetUserListRequest,
                Timestamp = DateTime.Now
            });
        }
        // USER
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
            // HEADER
            lblChatTarget.Text =
                username;
            bool isTargetOnline = false;
            if (_onlineUsersByName.TryGetValue(username, out var targetUser))
            {
                isTargetOnline = string.Equals(targetUser.Status, "Online", StringComparison.OrdinalIgnoreCase);
            }
            UpdateChatHeaderStatus(isTargetOnline);
            Image? targetAvatar = null;
            if (targetUser != null && !string.IsNullOrWhiteSpace(targetUser.Avatar))
            {
                targetAvatar = LoadAvatar(targetUser.Avatar);
            }
            targetAvatar ??= LoadAvatar("avt1.png");
            if (targetAvatar != null)
            {
                SetPictureBoxImage(
                    picChatAvatar,
                    targetAvatar);
            }
            // SESSION
            SessionManager.Instance
                .SetActiveDirectChat(
                    username,
                    tcpClient:
                        _tcpClient);
            // LOAD HISTORY
            flpMessages.Controls.Clear();
            _lastRenderedDate = null;
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
        // GROUP CHAT
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
            UpdateChatHeaderStatus(false);
            Image? groupAvatar = LoadAvatar("group.png") ?? LoadAvatar("avt2.png") ?? LoadAvatar("avt1.png");
            if (groupAvatar != null)
            {
                SetPictureBoxImage(
                    picChatAvatar,
                    groupAvatar);
            }
            int? groupId = null;
            if (_groupsByName.TryGetValue(groupName, out var groupObj) && groupObj.GroupId > 0)
            {
                groupId = groupObj.GroupId;
            }
            SessionManager.Instance
                .SetActiveGroupChat(
                    groupName,
                    groupId: groupId,
                    tcpClient:
                        _tcpClient);
            flpMessages.Controls.Clear();
            _lastRenderedDate = null;
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
        // CREATE GROUP
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
                    var created = createGroupForm.CreatedGroup;
                    _groupsByName[created.GroupName] = created;
                    AddGroupToList(
                        created.GroupName,
                        created);
                }
            }
        }
        // SEND MESSAGE
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
            if (_activeReplyMessage != null)
            {
                message.ReplyToMessageId =
                    _activeReplyMessage.Id;
                message.ReplyToSenderName =
                    _activeReplyMessage.SenderName;
                message.ReplyToContent =
                    _activeReplyMessage.Content;
            }
            // GROUP
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
                        content,
                        replyToId: message.ReplyToMessageId,
                        replyToSenderName: message.ReplyToSenderName,
                        replyToContent: message.ReplyToContent);
                }
                else
                {
                    _chatService.SendMessage(
                        message);
                }
            }
            else
            {
                // DIRECT CHAT
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
            ClearReplyTarget();
            // SESSION
            SessionManager.Instance
                .AddMessage(
                    message);
            // ADD TO UI
            AddMessageBubble(
                message,
                isMine: true);
            txtMessage.Clear();
            txtMessage.Focus();
        }
        // EMOJI
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
                _emojiPicker = new EmojiPickerForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill,
                    ShowInTaskbar = false
                };
                _emojiPicker.EmojiSelected += (emoji) =>
                {
                    InsertEmoji(emoji);
                };
                _emojiPicker.FormClosed += EmojiPicker_FormClosed;
                pnlEmoji.Controls.Clear();
                pnlEmoji.Controls.Add(_emojiPicker);
                pnlEmoji.Visible = true;
                _emojiPicker.Show();
                _emojiPicker.BringToFront();
                pnlInput.BringToFront();
            }
            catch
            {
                pnlEmoji.Visible = false;
            }
        }
        private void EmojiPicker_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            pnlEmoji.Visible = false;
            pnlEmoji.Controls.Clear();
            _emojiPicker = null;
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
        // LOGOUT
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
            IsLoggingOut = true;
            Close();
        }
        // FORM CLOSING
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
                if (_tcpClient != null)
                {
                    _tcpClient.MessageReceived -= OnTcpClientMessageReceived;
                    _tcpClient.Disconnect();
                }
                SessionManager.Instance.ClearSession(true);
            }
            catch
            {
            }
            if (!IsLoggingOut)
            {
                Application.Exit();
            }
        }
        // HISTORY
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
                    _lastRenderedDate = null;
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
        // RECEIVED MESSAGE
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
                bool isCurrentChat = false;
                if (isGroupMessage && _activeChatIsGroup)
                {
                    if (message.GroupId.HasValue && SessionManager.Instance.ActiveGroupId.HasValue)
                    {
                        isCurrentChat = message.GroupId.Value == SessionManager.Instance.ActiveGroupId.Value;
                    }
                    else
                    {
                        isCurrentChat = true;
                    }
                }
                else if (!isGroupMessage && !_activeChatIsGroup)
                {
                    isCurrentChat = string.Equals(
                        message.SenderName,
                        _activeChatTarget,
                        StringComparison.OrdinalIgnoreCase);
                }
                if (isCurrentChat)
                {
                    AddMessageBubble(
                        message,
                        isMine: false);
                }
            });
        }
        // USER STATUS
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
                if (_onlineUsersByName.TryGetValue(username, out var userObj))
                {
                    userObj.Status = isOnline ? "Online" : "Offline";
                }
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
                if (!_activeChatIsGroup && string.Equals(_activeChatTarget, username, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateChatHeaderStatus(isOnline);
                }
            });
        }
        private void UpdateChatHeaderStatus(bool isOnline)
        {
            if (_activeChatIsGroup)
            {
                lblChatStatus.Text = "Nhóm chat";
                lblChatStatus.ForeColor = Color.Gray;
            }
            else
            {
                lblChatStatus.Text = isOnline ? "Online" : "Offline";
                lblChatStatus.ForeColor = isOnline ? Color.SeaGreen : Color.Gray;
            }
        }
        // GROUP UPDATE
        public void OnGroupUpdated(
            string groupName)
        {
            InvokeIfRequired(
                () =>
                    AddGroupToList(
                        groupName));
        }
        // TCP CLIENT MESSAGE HANDLER (Users & Status)
        private void OnTcpClientMessageReceived(ChatMessage message)
        {
            switch (message.Type)
            {
                case MessageType.GetUserListResponse:
                    try
                    {
                        var users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(message.Content);
                        if (users != null)
                        {
                            InvokeIfRequired(() =>
                            {
                                foreach (var u in users)
                                {
                                    if (string.Equals(u.Username, _currentUsername, StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    bool isOnline = string.Equals(u.Status, "Online", StringComparison.OrdinalIgnoreCase);
                                    AddUserToList(u.Username, isOnline, u);
                                }
                            });
                        }
                    }
                    catch { }
                    break;
                case MessageType.UserStatusUpdate:
                    try
                    {
                        var user = System.Text.Json.JsonSerializer.Deserialize<User>(message.Content);
                        if (user != null)
                        {
                            InvokeIfRequired(() =>
                            {
                                if (string.Equals(user.Username, _currentUsername, StringComparison.OrdinalIgnoreCase))
                                    return;
                                bool isOnline = string.Equals(user.Status, "Online", StringComparison.OrdinalIgnoreCase);
                                AddUserToList(user.Username, isOnline, user);
                            });
                        }
                    }
                    catch { }
                    break;
                case MessageType.GetChatHistoryResponse:
                    try
                    {
                        if (message.GroupId.HasValue && message.GroupId.Value > 0)
                        {
                            var groupHist = System.Text.Json.JsonSerializer.Deserialize<GroupHistoryResponse>(message.Content);
                            if (groupHist != null && groupHist.Success && groupHist.Messages != null)
                            {
                                string key = SessionManager.GetGroupKey(message.GroupId.Value);
                                SessionManager.Instance.SetHistory(key, groupHist.Messages);
                            }
                        }
                        else
                        {
                            var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(message.Content);
                            if (messages != null)
                            {
                                string partner = !string.IsNullOrWhiteSpace(message.SenderName) && message.SenderName != "Server"
                                    ? message.SenderName
                                    : _activeChatTarget;
                                if (!string.IsNullOrWhiteSpace(partner))
                                {
                                    string key = SessionManager.GetDirectChatKey(partner);
                                    SessionManager.Instance.SetHistory(key, messages);
                                }
                            }
                        }
                    }
                    catch { }
                    break;
            }
        }
        // ADD USER
        private void AddUserToList(
            string username,
            bool isOnline,
            User? userObj = null)
        {
            if (string.Equals(
                username,
                _currentUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (userObj != null)
            {
                userObj.Status = isOnline ? "Online" : "Offline";
                _onlineUsersByName[username] = userObj;
            }
            else if (_onlineUsersByName.TryGetValue(username, out var existingUser))
            {
                existingUser.Status = isOnline ? "Online" : "Offline";
            }
            else
            {
                _onlineUsersByName[username] =
                    new User
                    {
                        UserId = _nextDemoUserId++,
                        Username = username,
                        DisplayName = username,
                        Status = isOnline ? "Online" : "Offline"
                    };
            }
            if (_userItems.ContainsKey(
                username))
            {
                UpdateUserStatus(
                    _userItems[username],
                    isOnline);
                if (!_activeChatIsGroup && string.Equals(_activeChatTarget, username, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateChatHeaderStatus(isOnline);
                    if (userObj != null && !string.IsNullOrWhiteSpace(userObj.Avatar))
                    {
                        var avt = LoadAvatar(userObj.Avatar) ?? LoadAvatar("avt1.png");
                        if (avt != null)
                        {
                            SetPictureBoxImage(picChatAvatar, avt);
                        }
                    }
                }
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
            if (!_activeChatIsGroup && string.Equals(_activeChatTarget, username, StringComparison.OrdinalIgnoreCase))
            {
                UpdateChatHeaderStatus(isOnline);
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
        // ADD GROUP
        private void AddGroupToList(
            string groupName,
            Group? groupObj = null)
        {
            if (groupObj != null)
            {
                _groupsByName[groupName] = groupObj;
            }
            else if (!_groupsByName.ContainsKey(groupName))
            {
                _groupsByName[groupName] =
                    new Group
                    {
                        GroupName = groupName
                    };
            }
            if (_groupItems.ContainsKey(groupName))
            {
                return;
            }
            var item =
                new ListViewItem(
                    groupName)
                {
                    Tag = _groupsByName[groupName]
                };
            lvGroups.Items.Add(
                item);
            _groupItems[groupName] =
                item;
        }
        private void OnGroupListReceived(
            List<Group> groups)
        {
            InvokeIfRequired(() =>
            {
                foreach (var g in groups)
                {
                    _groupsByName[g.GroupName] = g;
                    AddGroupToList(
                        g.GroupName,
                        g);
                }
            });
        }
        private void OnGroupCreated(
            Group group)
        {
            InvokeIfRequired(() =>
            {
                _groupsByName[group.GroupName] = group;
                AddGroupToList(
                    group.GroupName,
                    group);
            });
        }
        private void OnGroupMessageReceived(
            ChatMessage message)
        {
            OnMessageReceived(
                message,
                isGroupMessage: true);
        }
        // CHAT BUBBLE & DATE SEPARATOR
        private void AddDateSeparator(DateTime date)
        {
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
            var row = new Panel
            {
                Width = rowWidth,
                Height = 28,
                Margin = new Padding(0, 8, 0, 8),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
                Tag = "DATE_SEPARATOR"
            };
            string dateText;
            if (date.Date == DateTime.Today)
            {
                dateText = $"Hôm nay, {date:dd/MM/yyyy}";
            }
            else if (date.Date == DateTime.Today.AddDays(-1))
            {
                dateText = $"Hôm qua, {date:dd/MM/yyyy}";
            }
            else
            {
                dateText = date.ToString("dd/MM/yyyy");
            }
            var lblDate = new Label
            {
                Text = dateText,
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(235, 238, 242),
                Padding = new Padding(12, 4, 12, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "DATE_LABEL"
            };
            row.Controls.Add(lblDate);
            lblDate.PerformLayout();
            lblDate.Location = new Point(
                Math.Max(0, (row.Width - lblDate.Width) / 2),
                Math.Max(0, (row.Height - lblDate.Height) / 2));
            flpMessages.Controls.Add(row);
        }
        private void AddMessageBubble(
            ChatMessage message,
            bool isMine)
        {
            if (message.Timestamp == default)
            {
                message.Timestamp = DateTime.Now;
            }
            // Hiển thị dòng phân cách ngày nếu tin nhắn thuộc ngày mới
            if (_lastRenderedDate == null || _lastRenderedDate.Value.Date != message.Timestamp.Date)
            {
                AddDateSeparator(message.Timestamp.Date);
                _lastRenderedDate = message.Timestamp.Date;
            }
            var bubble =
                new ChatBubble();
            bubble.Tag = isMine ? "MINE" : "OTHER";
            bubble.SetMessage(
                message,
                isMine);
            bubble.ReplyClicked +=
                ChatBubble_ReplyClicked;
            bubble.ForwardClicked +=
                ChatBubble_ForwardClicked;
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
        // RESIZE MESSAGE ROWS
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
                if (row.Tag as string == "DATE_SEPARATOR" || bubble.Tag as string == "DATE_LABEL")
                {
                    bubble.Left = Math.Max(0, (row.Width - bubble.Width) / 2);
                    continue;
                }
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
        // REPLY
        private void ChatBubble_ReplyClicked(
            object? sender,
            ChatMessage repliedMessage)
        {
            SetReplyTarget(
                repliedMessage);
        }
        private void SetReplyTarget(
            ChatMessage repliedMessage)
        {
            _activeReplyMessage =
                repliedMessage;
            _chatService.SetReplyTarget(
                repliedMessage);
            string preview =
                repliedMessage.Content.Length > 40
                    ? repliedMessage.Content.Substring(0, 37) + "..."
                    : repliedMessage.Content;
            lblReplyPreview.Text =
                $"↩ Trả lời {repliedMessage.SenderName}: \"{preview}\"";
            pnlReplyPreview.Visible =
                true;
            txtMessage.Focus();
        }
        private void ClearReplyTarget()
        {
            _activeReplyMessage =
                null;
            _chatService.ClearReplyTarget();
            pnlReplyPreview.Visible =
                false;
        }
        private void BtnCancelReply_Click(
            object? sender,
            EventArgs e)
        {
            ClearReplyTarget();
        }
        // FORWARD
        private void ChatBubble_ForwardClicked(
            object? sender,
            ChatMessage messageToForward)
        {
            if (_tcpClient == null ||
                !_tcpClient.IsConnected)
            {
                MessageBox.Show(
                    "Client chưa kết nối đến Server.",
                    "Chuyển tiếp tin nhắn",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            var userList =
                _onlineUsersByName.Values
                    .Where(u =>
                        !string.Equals(
                            u.Username,
                            _currentUsername,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        (_activeChatIsGroup ||
                         !string.Equals(
                             u.Username,
                             _activeChatTarget,
                             StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            var groupList =
                _groupsByName.Values
                    .Where(g =>
                        !_activeChatIsGroup ||
                        !string.Equals(
                            g.GroupName,
                            _activeChatTarget,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            using var dlg =
                new ForwardDialog(
                    userList,
                    groupList,
                    messageToForward);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (dlg.IsTargetGroup)
                {
                    int? groupId = dlg.SelectedGroupId;
                    if ((!groupId.HasValue || groupId.Value <= 0) &&
                        _groupsByName.TryGetValue(dlg.SelectedGroupName, out var gObj) &&
                        gObj.GroupId > 0)
                    {
                        groupId = gObj.GroupId;
                    }
                    var forwardMsg =
                        new ChatMessage
                        {
                            SenderId =
                                _currentUser.UserId,
                            SenderName =
                                _currentUsername,
                            GroupId =
                                groupId,
                            Content =
                                messageToForward.Content,
                            Type =
                                MessageType.GroupChat,
                            IsForward =
                                true,
                            Timestamp =
                                DateTime.Now
                        };
                    if (_groupService != null && groupId.HasValue && groupId.Value > 0)
                    {
                        _groupService.SendGroupMessage(
                            groupId.Value,
                            messageToForward.Content,
                            isForward: true);
                    }
                    else
                    {
                        _chatService.SendMessage(
                            forwardMsg);
                    }
                    SessionManager.Instance
                        .AddGroupMessage(
                            dlg.SelectedGroupName,
                            groupId,
                            forwardMsg);
                    if (_activeChatIsGroup &&
                        string.Equals(
                            _activeChatTarget,
                            dlg.SelectedGroupName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        AddMessageBubble(
                            forwardMsg,
                            isMine: true);
                    }
                    MessageBox.Show(
                        $"Đã chuyển tiếp tin nhắn đến nhóm {dlg.TargetName}.",
                        "Chuyển tiếp tin nhắn",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    int? receiverId = dlg.SelectedReceiverId;
                    if ((!receiverId.HasValue || receiverId.Value <= 0) &&
                        _onlineUsersByName.TryGetValue(dlg.SelectedReceiverUsername, out var uObj) &&
                        uObj.UserId > 0)
                    {
                        receiverId = uObj.UserId;
                    }
                    var forwardMsg =
                        new ChatMessage
                        {
                            SenderId =
                                _currentUser.UserId,
                            SenderName =
                                _currentUsername,
                            ReceiverId =
                                receiverId,
                            Content =
                                messageToForward.Content,
                            Type =
                                MessageType.DirectChat,
                            IsForward =
                                true,
                            Timestamp =
                                DateTime.Now
                        };
                    _chatService.SendMessage(
                        forwardMsg);
                    SessionManager.Instance
                        .AddDirectMessage(
                            dlg.SelectedReceiverUsername,
                            forwardMsg);
                    if (!_activeChatIsGroup &&
                        string.Equals(
                            _activeChatTarget,
                            dlg.SelectedReceiverUsername,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        AddMessageBubble(
                            forwardMsg,
                            isMine: true);
                    }
                    MessageBox.Show(
                        $"Đã chuyển tiếp tin nhắn đến {dlg.TargetName}.",
                        "Chuyển tiếp tin nhắn",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }
        // SCROLL
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
        // INPUT LAYOUT
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
        // AVATAR LOADER
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
        // CIRCLE AVATAR
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
        // INITIALS
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
        // UI THREAD
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
