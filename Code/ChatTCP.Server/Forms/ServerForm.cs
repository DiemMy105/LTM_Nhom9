using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;


using ChatTCP.Server.Network;
using ChatTCP.Server.Services;
using ChatTCP.Server.Utils;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Utils;


// Tránh lỗi ambiguous reference giữa System.Windows.Forms.Message và ChatTCP.Shared.Models.Message
using Message = ChatTCP.Shared.Models.Message;


namespace ChatTCP.Server.Forms
{
    public partial class ServerForm : Form
    {
        // BACKEND


        private TcpServer? _tcpServer;
        private MessageHandler? _messageHandler;
        private DatabaseService? _databaseService;
        private ClientManager? _clientManager;
        private GroupManager? _groupManager;
        private Logger? _logger;


        private bool _isRunning = false;


        // UI CONTROLS


        private Panel pnlTop = null!;


        private Label lblIp = null!;
        private TextBox txtIp = null!;
        private Label lblPort = null!;
        private NumericUpDown numPort = null!;


        private Button btnStart = null!;
        private Button btnStop = null!;
        private Button btnRefreshIp = null!;


        private TabControl tabMain = null!;
        private TabPage tabClients = null!;
        private TabPage tabGroups = null!;


        private ListView lvClients = null!;
        private ListView lvGroups = null!;


        private ContextMenuStrip cmsClients = null!;
        private ToolStripMenuItem miKickClient = null!;
        private ToolStripMenuItem miDeleteUser = null!;


        private ContextMenuStrip cmsGroups = null!;
        private ToolStripMenuItem miViewGroupMembers = null!;


        private Panel pnlLog = null!;
        private Label lblLogTitle = null!;
        private RichTextBox rtbLog = null!;


        private Panel pnlLogButtons = null!;
        private Button btnClearLog = null!;
        private Button btnSaveLog = null!;


        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblServerStatus = null!;
        private ToolStripStatusLabel lblClientCount = null!;
        private ToolStripStatusLabel lblGroupCount = null!;


        // LIST CACHE (Key = UserId / GroupId)


        private readonly Dictionary<int, ListViewItem> _userItems =
            new Dictionary<int, ListViewItem>();


        private readonly Dictionary<int, ListViewItem> _groupItems =
            new Dictionary<int, ListViewItem>();


        // CONSTRUCTOR


        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }


        // UI SETUP


        private void InitializeComponent()
        {
            // FORM


            this.Text = "ChatTCP - Server Console";


            // Kích thước mặc định rộng rãi, thoải mái
            this.ClientSize = new Size(1100, 680);
            this.MinimumSize = new Size(950, 580);
            this.StartPosition = FormStartPosition.CenterScreen;


            this.Font = new Font(
                "Segoe UI",
                9F,
                FontStyle.Regular
            );


            // TOP PANEL


            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(0),
                BackColor = SystemColors.Control
            };


            // IP Label & TextBox
            lblIp = new Label
            {
                Text = "IP:",
                Location = new Point(14, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };


            txtIp = new TextBox
            {
                Location = new Point(42, 16),
                Size = new Size(125, 27),
                Text = GetLocalIPAddress(),
                ReadOnly = true
            };


            // Refresh IP Button
            btnRefreshIp = new Button
            {
                Text = "⟳",
                Location = new Point(173, 15),
                Size = new Size(32, 29),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefreshIp.Click += BtnRefreshIp_Click;


            // Port Label & NumericUpDown
            lblPort = new Label
            {
                Text = "Port:",
                Location = new Point(225, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };


            numPort = new NumericUpDown
            {
                Location = new Point(268, 16),
                Size = new Size(95, 27),
                Minimum = 1024,
                Maximum = 65535,
                Value = NetworkConfig.ServerPort,
                DecimalPlaces = 0,
                ThousandsSeparator = false,
                TextAlign = HorizontalAlignment.Left,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };


            // Start Server Button
            btnStart = new Button
            {
                Text = "▶ Start",
                Location = new Point(380, 13),
                Size = new Size(115, 34),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;


            // Stop Server Button
            btnStop = new Button
            {
                Text = "■ Stop",
                Location = new Point(505, 13),
                Size = new Size(115, 34),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += BtnStop_Click;


            pnlTop.Controls.AddRange(new Control[]
            {
                lblIp,
                txtIp,
                btnRefreshIp,
                lblPort,
                numPort,
                btnStart,
                btnStop
            });


            // CLIENT CONTEXT MENU


            cmsClients = new ContextMenuStrip();

            miKickClient = new ToolStripMenuItem("⚡ Ngắt kết nối (Disconnect)");
            miKickClient.Click += MiKickClient_Click;

            miDeleteUser = new ToolStripMenuItem("🗑 Xóa người dùng (Delete User)");
            miDeleteUser.Click += MiDeleteUser_Click;

            cmsClients.Items.Add(miKickClient);
            cmsClients.Items.Add(miDeleteUser);

            // GROUP CONTEXT MENU

            cmsGroups = new ContextMenuStrip();

            miViewGroupMembers = new ToolStripMenuItem("Xem thành viên nhóm");
            miViewGroupMembers.Click += MiViewGroupMembers_Click;

            var miRefreshGroups = new ToolStripMenuItem("Làm mới danh sách nhóm");
            miRefreshGroups.Click += (s, e) =>
            {
                LoadAllGroupsFromDatabase();
                AppendLog("[SYSTEM] Đã làm mới danh sách nhóm chat.");
            };

            cmsGroups.Items.Add(miViewGroupMembers);
            cmsGroups.Items.Add(new ToolStripSeparator());
            cmsGroups.Items.Add(miRefreshGroups);

            // TAB CONTROL

            tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Tab 1: Clients / Users
            tabClients = new TabPage("Danh sách người dùng");

            lvClients = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                ContextMenuStrip = cmsClients,
                HideSelection = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            lvClients.Columns.Add("ID", 70, HorizontalAlignment.Center);
            lvClients.Columns.Add("Username", 170, HorizontalAlignment.Left);
            lvClients.Columns.Add("Tên hiển thị", 200, HorizontalAlignment.Left);
            lvClients.Columns.Add("Trạng thái", 140, HorizontalAlignment.Left);

            lvClients.MouseDown += LvClients_MouseDown;

            // CLIENTS ACTION TOOLBAR
            var pnlClientsAction = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(8, 6, 8, 6),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            var btnDisconnectClient = new Button
            {
                Text = "⚡ Ngắt kết nối",
                Width = 145,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 130, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnDisconnectClient.FlatAppearance.BorderSize = 0;
            btnDisconnectClient.Click += MiKickClient_Click;

            var btnDeleteUser = new Button
            {
                Text = "🗑 Xóa người dùng",
                Width = 155,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(215, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnDeleteUser.FlatAppearance.BorderSize = 0;
            btnDeleteUser.Click += MiDeleteUser_Click;

            pnlClientsAction.Controls.AddRange(new Control[]
            {
                btnDisconnectClient,
                btnDeleteUser
            });

            tabClients.Controls.Add(lvClients);
            tabClients.Controls.Add(pnlClientsAction);


            // Tab 2: Groups
            tabGroups = new TabPage("Nhóm chat (Groups)");


            lvGroups = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                ContextMenuStrip = cmsGroups,
                HideSelection = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };


            lvGroups.Columns.Add("ID", 70, HorizontalAlignment.Center);
            lvGroups.Columns.Add("Tên nhóm", 220, HorizontalAlignment.Left);
            lvGroups.Columns.Add("Chủ nhóm", 180, HorizontalAlignment.Left);
            lvGroups.Columns.Add("Số thành viên", 120, HorizontalAlignment.Center);


            lvGroups.MouseDown += LvGroups_MouseDown;
            tabGroups.Controls.Add(lvGroups);


            tabMain.TabPages.Add(tabClients);
            tabMain.TabPages.Add(tabGroups);


            // LOG PANEL


            pnlLog = new Panel
            {
                Dock = DockStyle.Right,
                Width = 480,
                Padding = new Padding(6)
            };


            lblLogTitle = new Label
            {
                Text = "Nhật ký hoạt động (Server Logs)",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(4, 0, 0, 0)
            };


            pnlLogButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(0, 6, 0, 0)
            };


            btnClearLog = new Button
            {
                Text = "Xóa Log",
                Location = new Point(4, 8),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnClearLog.Click += BtnClearLog_Click;


            btnSaveLog = new Button
            {
                Text = "Lưu File Log",
                Location = new Point(112, 8),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnSaveLog.Click += BtnSaveLog_Click;


            pnlLogButtons.Controls.Add(btnClearLog);
            pnlLogButtons.Controls.Add(btnSaveLog);


            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 9.5F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };


            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(pnlLogButtons);
            pnlLog.Controls.Add(lblLogTitle);


            // STATUS STRIP


            statusStrip = new StatusStrip();


            lblServerStatus = new ToolStripStatusLabel("● Server: Offline")
            {
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };


            lblClientCount = new ToolStripStatusLabel("Online: 0 / Tổng: 0")
            {
                Spring = false
            };


            lblGroupCount = new ToolStripStatusLabel("Groups: 0")
            {
                Spring = false
            };


            var spacer = new ToolStripStatusLabel
            {
                Spring = true
            };


            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                lblServerStatus,
                spacer,
                lblClientCount,
                new ToolStripSeparator(),
                lblGroupCount
            });


            // ADD CONTROLS TO FORM


            this.Controls.Add(tabMain);
            this.Controls.Add(pnlLog);
            this.Controls.Add(pnlTop);
            this.Controls.Add(statusStrip);


            this.FormClosing += ServerForm_FormClosing;
        }


        // BACKEND INITIALIZATION


        private void InitializeServerComponents()
        {
            _databaseService = new DatabaseService();
            _clientManager = new ClientManager();
            _groupManager = new GroupManager(_databaseService);


            _messageHandler = new MessageHandler(
                _databaseService,
                _clientManager,
                _groupManager
            );


            _tcpServer = new TcpServer();


            // TCP Server events
            _tcpServer.ClientConnected += TcpServer_ClientConnected;
            _tcpServer.MessageReceived += TcpServer_MessageReceived;


            // MessageHandler events
            _messageHandler.UserLoggedIn += MessageHandler_UserLoggedIn;
            _messageHandler.UserRegistered += MessageHandler_UserRegistered;
            _messageHandler.UserLoggedOut += MessageHandler_UserLoggedOut;


            // GroupManager events
            _groupManager.GroupCreated += GroupManager_GroupCreated;
            _groupManager.GroupUpdated += GroupManager_GroupUpdated;
            _groupManager.GroupDissolved += GroupManager_GroupDissolved;


            _logger = Logger.Instance;


            AppendLog("[SYSTEM] Server Console đã khởi tạo. Nhấn [Start] để bắt đầu lắng nghe.");


            // Tải danh sách người dùng và nhóm chat ban đầu từ CSDL
            LoadAllUsersFromDatabase();
            LoadAllGroupsFromDatabase();
        }


        // TCP SERVER EVENTS


        private void TcpServer_ClientConnected(ClientConnection connection)
        {
            AppendLog("[CONNECT] Một Client mới đã kết nối tới Server.");
            connection.Disconnected += TcpServer_ClientDisconnected;
        }


        private void TcpServer_ClientDisconnected(ClientConnection connection)
        {
            _clientManager?.RemoveClient(connection);


            if (connection.UserId > 0)
            {
                _databaseService?.UpdateUserStatus(connection.UserId, "Offline");


                // Broadcast trạng thái Offline tới các Client khác
                var statusMsg = new Message
                {
                    SenderId = connection.UserId,
                    SenderName = connection.Username ?? string.Empty,
                    Type = ChatTCP.Shared.Enums.MessageType.UserStatusUpdate,
                    Content = System.Text.Json.JsonSerializer.Serialize(new User
                    {
                        UserId = connection.UserId,
                        Username = connection.Username ?? string.Empty,
                        Status = "Offline"
                    }),
                    Timestamp = DateTime.Now
                };
                _clientManager?.Broadcast(statusMsg);
            }


            InvokeIfRequired(() =>
            {
                if (connection.UserId > 0 && !string.IsNullOrEmpty(connection.Username))
                {
                    SetUserOffline(connection.UserId);
                    AppendLog($"[DISCONNECT] \"{connection.Username}\" (ID: {connection.UserId}) đã ngắt kết nối.");
                }
                else
                {
                    AppendLog("[DISCONNECT] Một Client đã ngắt kết nối (chưa đăng nhập).");
                }
            });
        }


        private void TcpServer_MessageReceived(ClientConnection connection, Message message)
        {
            AppendLog($"[MSG] Nhận {message.Type} từ Client.");
            _messageHandler?.HandleIncomingMessage(message, connection);
        }


        // USER AUTHENTICATION / STATUS EVENTS


        private void MessageHandler_UserLoggedIn(User user, ClientConnection client)
        {
            InvokeIfRequired(() =>
            {
                UpdateOrAddUserInList(
                    user.UserId,
                    user.Username,
                    user.DisplayName,
                    true
                );


                AppendLog($"[LOGIN] Người dùng \"{user.Username}\" (ID: {user.UserId}) đã đăng nhập.");
            });
        }


        private void MessageHandler_UserRegistered(User user)
        {
            InvokeIfRequired(() =>
            {
                UpdateOrAddUserInList(
                    user.UserId,
                    user.Username,
                    user.DisplayName,
                    false
                );


                AppendLog($"[REGISTER] Tài khoản mới \"{user.Username}\" (ID: {user.UserId}) vừa được đăng ký.");
            });
        }


        private void MessageHandler_UserLoggedOut(int userId, string username)
        {
            InvokeIfRequired(() =>
            {
                SetUserOffline(userId);
                AppendLog($"[LOGOUT] Người dùng \"{username}\" (ID: {userId}) đã đăng xuất.");
            });
        }


        // USER LIST MANAGEMENT (Real-time Online/Offline)


        private void LoadAllUsersFromDatabase()
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    var users = (_databaseService?.GetAllUsers() ?? new List<User>())
                        .OrderBy(u => u.UserId)
                        .ToList();
                    lvClients.BeginUpdate();
                    lvClients.Items.Clear();
                    _userItems.Clear();


                    foreach (var u in users)
                    {
                        var onlineClient = _clientManager?.GetClient(u.UserId);
                        bool isOnline = onlineClient != null && onlineClient.IsConnected;
                        string status = isOnline ? "● Online" : "● Offline";


                        var item = new ListViewItem(new[]
                        {
                            u.UserId.ToString(),
                            u.Username,
                            u.DisplayName,
                            status
                        })
                        {
                            Tag = u.UserId,
                            UseItemStyleForSubItems = false
                        };


                        item.SubItems[3].ForeColor = isOnline ? Color.ForestGreen : Color.Crimson;
                        item.SubItems[3].Font = new Font(this.Font, FontStyle.Bold);


                        lvClients.Items.Add(item);
                        _userItems[u.UserId] = item;
                    }


                    lvClients.EndUpdate();
                    UpdateClientCount();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "LoadAllUsersFromDatabase");
                }
            });
        }


        private void UpdateOrAddUserInList(
            int userId,
            string username,
            string displayName,
            bool isOnline)
        {
            string statusText = isOnline ? "● Online" : "● Offline";
            Color statusColor = isOnline ? Color.ForestGreen : Color.Crimson;


            if (_userItems.TryGetValue(userId, out var item))
            {
                item.SubItems[1].Text = username;
                item.SubItems[2].Text = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
                item.SubItems[3].Text = statusText;
                item.SubItems[3].ForeColor = statusColor;
                item.SubItems[3].Font = new Font(this.Font, FontStyle.Bold);
            }
            else
            {
                item = new ListViewItem(new[]
                {
                    userId.ToString(),
                    username,
                    string.IsNullOrWhiteSpace(displayName) ? username : displayName,
                    statusText
                })
                {
                    Tag = userId,
                    UseItemStyleForSubItems = false
                };


                item.SubItems[3].ForeColor = statusColor;
                item.SubItems[3].Font = new Font(this.Font, FontStyle.Bold);


                // Chèn đúng vị trí tăng dần theo UserId
                int targetIndex = 0;
                while (targetIndex < lvClients.Items.Count &&
                       int.TryParse(lvClients.Items[targetIndex].Text, out int existingId) &&
                       existingId < userId)
                {
                    targetIndex++;
                }


                lvClients.Items.Insert(targetIndex, item);
                _userItems[userId] = item;
            }


            UpdateClientCount();
        }


        private void SetUserOffline(int userId)
        {
            if (_userItems.TryGetValue(userId, out var item))
            {
                item.SubItems[3].Text = "● Offline";
                item.SubItems[3].ForeColor = Color.Crimson;
                item.SubItems[3].Font = new Font(this.Font, FontStyle.Bold);
            }


            UpdateClientCount();
        }


        private void UpdateClientCount()
        {
            int onlineCount = _clientManager?.Count ?? 0;
            int totalCount = _userItems.Count;
            lblClientCount.Text = $"Online: {onlineCount} / Tổng: {totalCount}";
        }


        // GROUP EVENTS


        private void GroupManager_GroupCreated(Group group)
        {
            InvokeIfRequired(() =>
            {
                AddOrUpdateGroupInList(group);
                AppendLog($"[GROUP] Nhóm \"{group.GroupName}\" được tạo bởi \"{ResolveUsername(group.CreatedBy)}\" ({group.MemberIds.Count} thành viên).");
            });
        }


        private void GroupManager_GroupUpdated(Group group)
        {
            InvokeIfRequired(() =>
            {
                AddOrUpdateGroupInList(group);
                AppendLog($"[GROUP] Nhóm \"{group.GroupName}\" vừa được cập nhật ({group.MemberIds.Count} thành viên).");
            });
        }


        private void GroupManager_GroupDissolved(Group group)
        {
            InvokeIfRequired(() =>
            {
                RemoveGroupFromList(group.GroupId);
                AppendLog($"[GROUP] Nhóm \"{group.GroupName}\" đã bị giải tán.");
            });
        }


        private void LoadAllGroupsFromDatabase()
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    var groups = _groupManager?.GetGroups() ?? _databaseService?.GetAllGroups() ?? new List<Group>();
                    lvGroups.BeginUpdate();
                    lvGroups.Items.Clear();
                    _groupItems.Clear();


                    foreach (var group in groups)
                    {
                        string ownerName = ResolveUsername(group.CreatedBy);


                        var item = new ListViewItem(new[]
                        {
                            group.GroupId.ToString(),
                            group.GroupName,
                            ownerName,
                            group.MemberIds.Count.ToString()
                        })
                        {
                            Tag = group.GroupId
                        };


                        lvGroups.Items.Add(item);
                        _groupItems[group.GroupId] = item;
                    }


                    lvGroups.EndUpdate();
                    lblGroupCount.Text = $"Groups: {_groupItems.Count}";
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "LoadAllGroupsFromDatabase");
                }
            });
        }


        private void AddOrUpdateGroupInList(Group group)
        {
            string ownerName = ResolveUsername(group.CreatedBy);


            if (_groupItems.TryGetValue(group.GroupId, out var existing))
            {
                existing.SubItems[1].Text = group.GroupName;
                existing.SubItems[2].Text = ownerName;
                existing.SubItems[3].Text = group.MemberIds.Count.ToString();
                return;
            }


            var item = new ListViewItem(new[]
            {
                group.GroupId.ToString(),
                group.GroupName,
                ownerName,
                group.MemberIds.Count.ToString()
            })
            {
                Tag = group.GroupId
            };


            lvGroups.Items.Add(item);
            _groupItems[group.GroupId] = item;
            lblGroupCount.Text = $"Groups: {_groupItems.Count}";
        }


        private void RemoveGroupFromList(int groupId)
        {
            if (_groupItems.TryGetValue(groupId, out var item))
            {
                lvGroups.Items.Remove(item);
                _groupItems.Remove(groupId);
                lblGroupCount.Text = $"Groups: {_groupItems.Count}";
            }
        }


        private string ResolveUsername(int userId)
        {
            var onlineClient = _clientManager?.GetClient(userId);
            if (!string.IsNullOrEmpty(onlineClient?.Username))
            {
                return onlineClient!.Username;
            }


            var dbUser = _databaseService?.GetUserById(userId);
            if (!string.IsNullOrEmpty(dbUser?.Username))
            {
                return dbUser!.Username;
            }


            return $"UserId #{userId}";
        }


        // START / STOP SERVER


        private void BtnStart_Click(object? sender, EventArgs e)
        {
            int port = (int)numPort.Value;


            try
            {
                bool started = _tcpServer != null && _tcpServer.Start(port);


                if (!started)
                {
                    AppendLog($"[ERROR] Không thể khởi động Server tại cổng {port}.");
                    MessageBox.Show(
                        $"Không thể khởi động Server tại cổng {port}.\n\n" +
                        "Cổng có thể đang bị chiếm bởi một tiến trình khác.\n" +
                        "Kiểm tra Task Manager hoặc đổi sang cổng khác.",
                        "Không thể khởi động Server",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }


                _isRunning = true;

                // Ghi nhớ cấu hình port cho các lần khởi động sau
                NetworkConfig.ServerPort = port;
                NetworkConfig.Save();

                btnStart.Enabled = false;
                btnStop.Enabled = true;

                numPort.Enabled = false;
                btnRefreshIp.Enabled = false;

                lblServerStatus.Text = "● Server: Online";
                lblServerStatus.ForeColor = Color.LimeGreen;


                // Tải lại danh sách người dùng và nhóm chat khi bật server
                LoadAllUsersFromDatabase();
                LoadAllGroupsFromDatabase();


                AppendLog($"[SYSTEM] Server đã khởi động tại {txtIp.Text}:{port}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "BtnStart_Click");
                AppendLog($"[ERROR] Không thể khởi động Server: {ex.Message}");
                MessageBox.Show(
                    $"Không thể khởi động Server:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _tcpServer?.Stop();
            _isRunning = false;


            btnStart.Enabled = true;
            btnStop.Enabled = false;


            numPort.Enabled = true;
            btnRefreshIp.Enabled = true;


            lblServerStatus.Text = "● Server: Offline";
            lblServerStatus.ForeColor = Color.Red;


            // Đưa toàn bộ trạng thái trong danh sách về Offline
            foreach (var item in _userItems.Values)
            {
                item.SubItems[3].Text = "● Offline";
                item.SubItems[3].ForeColor = Color.Crimson;
            }


            UpdateClientCount();
            AppendLog("[SYSTEM] Server đã dừng.");
        }


        private void BtnRefreshIp_Click(object? sender, EventArgs e)
        {
            txtIp.Text = GetLocalIPAddress();
            AppendLog("[SYSTEM] Đã làm mới địa chỉ IP.");
        }


        private void ServerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_isRunning)
                return;


            var result = MessageBox.Show(
                "Server đang chạy. Bạn có chắc muốn thoát?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );


            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }


            _tcpServer?.Stop();
        }


        // CONTEXT MENUS & ACTIONS


        private void LvClients_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item = lvClients.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }


        private void MiKickClient_Click(object? sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn một người dùng để ngắt kết nối.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var item = lvClients.SelectedItems[0];
            int userId = item.Tag is int id ? id : (int.TryParse(item.SubItems[0].Text, out int parsedId) ? parsedId : 0);
            string username = item.SubItems[1].Text;
            string status = item.SubItems[3].Text;

            if (!status.Contains("Online"))
            {
                MessageBox.Show(
                    $"Người dùng \"{username}\" hiện đang Offline.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn ngắt kết nối người dùng \"{username}\" (ID: {userId})?",
                "Xác nhận ngắt kết nối",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            // 1. Gửi thông báo trực tiếp đến client bị ngắt kết nối trước khi đóng socket
            var client = _clientManager?.GetClient(userId) ?? _clientManager?.GetClient(username);
            if (client != null)
            {
                var kickNotice = new Message
                {
                    SenderId = 0,
                    SenderName = "Server",
                    ReceiverId = userId,
                    Type = ChatTCP.Shared.Enums.MessageType.SystemNotification,
                    Content = "KICKED:Bạn đã bị ngắt kết nối khỏi máy chủ bởi Quản trị viên (Admin).",
                    Timestamp = DateTime.Now
                };
                client.SendMessage(kickNotice);
                System.Threading.Thread.Sleep(80);

                _clientManager?.RemoveClient(client);
                client.Disconnect();
            }
            else
            {
                _clientManager?.DisconnectClient(username);
                _clientManager?.RemoveClient(userId);
            }

            // 2. Cập nhật Database
            if (userId > 0)
            {
                _databaseService?.UpdateUserStatus(userId, "Offline");
            }

            // 3. Chuyển trạng thái giao diện Server ngay lập tức về Offline
            SetUserOffline(userId);

            // 4. Broadcast thông báo Offline tới các Client khác
            var statusMsg = new Message
            {
                SenderId = userId,
                SenderName = username,
                Type = ChatTCP.Shared.Enums.MessageType.UserStatusUpdate,
                Content = System.Text.Json.JsonSerializer.Serialize(new User
                {
                    UserId = userId,
                    Username = username,
                    Status = "Offline"
                }),
                Timestamp = DateTime.Now
            };
            _clientManager?.Broadcast(statusMsg);

            AppendLog($"[DISCONNECT] Đã ngắt kết nối người dùng \"{username}\" (ID: {userId}) bởi Admin.");
        }


        private void MiDeleteUser_Click(object? sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn một người dùng để xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var item = lvClients.SelectedItems[0];
            int userId = item.Tag is int id ? id : (int.TryParse(item.SubItems[0].Text, out int parsedId) ? parsedId : 0);
            string username = item.SubItems[1].Text;

            if (userId <= 0)
            {
                MessageBox.Show(
                    "Không thể xác định UserId của người dùng cần xóa.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            var confirm = MessageBox.Show(
                $"CẢNH BÁO: Xóa người dùng \"{username}\" (ID: {userId}) sẽ xóa TOÀN BỘ dữ liệu liên quan (tin nhắn, lịch sử chat, tham gia nhóm) khỏi cơ sở dữ liệu!\n\nBạn có chắc chắn muốn xóa không?",
                "Xác nhận xóa người dùng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop
            );

            if (confirm != DialogResult.Yes)
                return;

            // 1. Nếu client đang online, gửi thông báo tài khoản bị xóa và ngắt kết nối
            var client = _clientManager?.GetClient(userId) ?? _clientManager?.GetClient(username);
            if (client != null)
            {
                var deleteNotice = new Message
                {
                    SenderId = 0,
                    SenderName = "Server",
                    ReceiverId = userId,
                    Type = ChatTCP.Shared.Enums.MessageType.SystemNotification,
                    Content = "DELETED:Tài khoản của bạn đã bị Quản trị viên xóa hoàn toàn khỏi hệ thống.",
                    Timestamp = DateTime.Now
                };
                client.SendMessage(deleteNotice);
                System.Threading.Thread.Sleep(80);

                _clientManager?.RemoveClient(client);
                client.Disconnect();
            }
            else
            {
                _clientManager?.DisconnectClient(username);
                _clientManager?.RemoveClient(userId);
            }

            // 2. Xóa toàn bộ dữ liệu người dùng khỏi Database
            if (_databaseService != null && !_databaseService.DeleteUser(userId, out string dbError))
            {
                MessageBox.Show(
                    $"Xóa người dùng khỏi CSDL thất bại:\n{dbError}",
                    "Lỗi CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // 3. Broadcast thông báo User đã bị xóa tới các Client còn lại
            var deleteMsg = new Message
            {
                SenderId = userId,
                SenderName = username,
                Type = ChatTCP.Shared.Enums.MessageType.UserStatusUpdate,
                Content = System.Text.Json.JsonSerializer.Serialize(new User
                {
                    UserId = userId,
                    Username = username,
                    Status = "Deleted"
                }),
                Timestamp = DateTime.Now
            };
            _clientManager?.Broadcast(deleteMsg);

            // 4. Xóa ngay lập tức khỏi giao diện Server
            if (_userItems.TryGetValue(userId, out var listItem))
            {
                lvClients.Items.Remove(listItem);
                _userItems.Remove(userId);
            }
            UpdateClientCount();

            // 5. Cập nhật lại danh sách nhóm (nếu user có trong các nhóm)
            LoadAllGroupsFromDatabase();

            AppendLog($"[DELETE USER] Đã xóa vĩnh viễn người dùng \"{username}\" (ID: {userId}) và toàn bộ dữ liệu liên quan khỏi hệ thống.");
            MessageBox.Show(
                $"Đã xóa người dùng \"{username}\" thành công.",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }


        private void LvGroups_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item = lvGroups.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }


        private void MiViewGroupMembers_Click(object? sender, EventArgs e)
        {
            if (lvGroups.SelectedItems.Count == 0)
                return;


            var item = lvGroups.SelectedItems[0];
            string groupName = item.SubItems[1].Text;


            if (item.Tag is not int groupId || _groupManager == null)
            {
                MessageBox.Show(
                    "Không xác định được nhóm đã chọn.",
                    "Thành viên nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }


            Group? group = _groupManager.GetGroupById(groupId);
            if (group == null)
            {
                MessageBox.Show(
                    $"Nhóm \"{groupName}\" không còn tồn tại.",
                    "Thành viên nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }


            string memberList = string.Join(
                "\n",
                group.MemberIds.Select(id =>
                    id == group.CreatedBy
                        ? $"{ResolveUsername(id)} (Trưởng nhóm)"
                        : ResolveUsername(id)
                )
            );


            MessageBox.Show(
                $"Nhóm \"{group.GroupName}\" - {group.MemberIds.Count} thành viên:\n\n" + memberList,
                "Thành viên nhóm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }


        // LOG METHODS


        private void AppendLog(string message)
        {
            InvokeIfRequired(() =>
            {
                Color color = Color.FromArgb(200, 225, 245); // Mặc định sáng dễ nhìn


                if (message.Contains("[ERROR]"))
                {
                    color = Color.FromArgb(255, 110, 110); // Đỏ san hô
                }
                else if (message.Contains("[DISCONNECT]") || message.Contains("[LOGOUT]"))
                {
                    color = Color.FromArgb(255, 175, 75); // Cam
                }
                else if (message.Contains("[CONNECT]") || message.Contains("[LOGIN]"))
                {
                    color = Color.FromArgb(120, 225, 120); // Xanh lá sáng
                }
                else if (message.Contains("[REGISTER]"))
                {
                    color = Color.FromArgb(190, 160, 240); // Tím nhạt
                }
                else if (message.Contains("[SYSTEM]"))
                {
                    color = Color.FromArgb(110, 195, 235); // Xanh dương nhạt
                }
                else if (message.Contains("[GROUP]"))
                {
                    color = Color.FromArgb(245, 215, 80); // Vàng sáng
                }


                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionLength = 0;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                rtbLog.SelectionColor = rtbLog.ForeColor;
                rtbLog.ScrollToCaret();
            });


            _logger?.Log(message);
        }


        private void BtnClearLog_Click(object? sender, EventArgs e)
        {
            rtbLog.Clear();
        }


        private void BtnSaveLog_Click(object? sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt",
                FileName = $"ServerLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.WriteAllText(sfd.FileName, rtbLog.Text);
                        MessageBox.Show(
                            "Đã lưu Log thành công.",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "BtnSaveLog_Click");
                        MessageBox.Show(
                            $"Không thể lưu Log:\n{ex.Message}",
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }


        // UTILITIES


        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Fallback nếu không lấy được IP
            }


            return "127.0.0.1";
        }


        private void InvokeIfRequired(Action action)
        {
            if (this.IsDisposed || this.Disposing)
            {
                return;
            }


            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(action);
                }
                catch
                {
                    // Form đang đóng
                }
            }
            else
            {
                action();
            }
        }
    }
}



