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

// Tránh lỗi:
// 'Message' is an ambiguous reference between
// 'System.Windows.Forms.Message' and 'ChatTCP.Shared.Models.Message'
using Message = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Server.Forms
{
    public partial class ServerForm : Form
    {
        // =========================================================
        // BACKEND
        // =========================================================

        private TcpServer? _tcpServer;
        private MessageHandler? _messageHandler;
        private DatabaseService? _databaseService;
        private ClientManager? _clientManager;
        private GroupManager? _groupManager;
        private Logger? _logger;

        private bool _isRunning = false;

        // =========================================================
        // UI CONTROLS
        // =========================================================

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
        private ToolStripMenuItem miViewClientInfo = null!;

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

        // =========================================================
        // LIST CACHE
        // =========================================================

        private readonly Dictionary<string, ListViewItem> _clientItems =
            new Dictionary<string, ListViewItem>();

        private readonly Dictionary<int, ListViewItem> _groupItems =
            new Dictionary<int, ListViewItem>();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        // =========================================================
        // UI SETUP
        // =========================================================

        private void InitializeComponent()
        {
            // -----------------------------------------------------
            // FORM
            // -----------------------------------------------------

            this.Text = "ChatTCP - Server Console";

            // Tăng kích thước một chút để các control không bị ép
            this.ClientSize = new Size(900, 600);

            this.MinimumSize = new Size(800, 520);

            this.StartPosition = FormStartPosition.CenterScreen;

            this.Font = new Font(
                "Segoe UI",
                9F,
                FontStyle.Regular
            );

            // -----------------------------------------------------
            // TOP PANEL
            // -----------------------------------------------------

            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(0)
            };

            // =====================================================
            // IP
            // =====================================================

            lblIp = new Label
            {
                Text = "IP:",
                Location = new Point(8, 20),
                AutoSize = true
            };

            txtIp = new TextBox
            {
                Location = new Point(35, 15),
                Size = new Size(105, 27),
                Text = GetLocalIPAddress(),
                ReadOnly = true
            };

            // -----------------------------------------------------
            // REFRESH IP
            // -----------------------------------------------------

            btnRefreshIp = new Button
            {
                Text = "⟳",
                Location = new Point(145, 14),
                Size = new Size(30, 28),
                FlatStyle = FlatStyle.Flat
            };

            btnRefreshIp.Click += BtnRefreshIp_Click;

            // =====================================================
            // PORT
            // =====================================================

            lblPort = new Label
            {
                Text = "Port:",
                Location = new Point(190, 20),
                AutoSize = true
            };

            // Quan trọng:
            // Đưa NumericUpDown sang x = 235
            // để không bị chữ "Port:" đè lên.
            numPort = new NumericUpDown
            {
                Location = new Point(235, 13),

                // Rộng hơn để số 8888 hiển thị rõ
                Size = new Size(105, 31),

                Minimum = 1024,
                Maximum = 65535,

                Value = NetworkConfig.ServerPort,

                DecimalPlaces = 0,
                ThousandsSeparator = false,

                // Cho số nằm bên trái, không sát mép
                TextAlign = HorizontalAlignment.Left,

                Font = new Font(
                    "Segoe UI",
                    9F,
                    FontStyle.Regular
                )
            };

            // =====================================================
            // START
            // =====================================================

            btnStart = new Button
            {
                Text = "Start",
                Location = new Point(350, 12),
                Size = new Size(110, 32),

                BackColor = Color.MediumSeaGreen,
                ForeColor = Color.White,

                FlatStyle = FlatStyle.Flat
            };

            btnStart.Click += BtnStart_Click;

            // =====================================================
            // STOP
            // =====================================================

            btnStop = new Button
            {
                Text = "Stop",
                Location = new Point(470, 12),
                Size = new Size(110, 32),

                BackColor = Color.IndianRed,
                ForeColor = Color.White,

                FlatStyle = FlatStyle.Flat,

                Enabled = false
            };

            btnStop.Click += BtnStop_Click;

            // -----------------------------------------------------
            // ADD TOP CONTROLS
            // -----------------------------------------------------

            pnlTop.Controls.AddRange(
                new Control[]
                {
                    lblIp,
                    txtIp,
                    btnRefreshIp,

                    lblPort,
                    numPort,

                    btnStart,
                    btnStop
                }
            );

            // =====================================================
            // CLIENT CONTEXT MENU
            // =====================================================

            cmsClients = new ContextMenuStrip();

            miViewClientInfo = new ToolStripMenuItem(
                "Xem thông tin"
            );

            miViewClientInfo.Click += MiViewClientInfo_Click;

            miKickClient = new ToolStripMenuItem(
                "Ngắt kết nối (Kick)"
            );

            miKickClient.Click += MiKickClient_Click;

            cmsClients.Items.Add(miViewClientInfo);
            cmsClients.Items.Add(miKickClient);

            // =====================================================
            // GROUP CONTEXT MENU
            // =====================================================

            cmsGroups = new ContextMenuStrip();

            miViewGroupMembers = new ToolStripMenuItem(
                "Xem thành viên nhóm"
            );

            miViewGroupMembers.Click += MiViewGroupMembers_Click;

            cmsGroups.Items.Add(miViewGroupMembers);

            // =====================================================
            // TAB CONTROL
            // =====================================================

            tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // =====================================================
            // CLIENT TAB
            // =====================================================

            tabClients = new TabPage(
                "Clients Online"
            );

            lvClients = new ListView
            {
                Dock = DockStyle.Fill,

                View = View.Details,

                FullRowSelect = true,

                GridLines = true,

                ContextMenuStrip = cmsClients,

                HideSelection = false
            };

            lvClients.Columns.Add(
                "Username",
                150
            );

            lvClients.Columns.Add(
                "IP Address",
                130
            );

            lvClients.Columns.Add(
                "Trạng thái",
                100
            );

            lvClients.Columns.Add(
                "Thời gian kết nối",
                150
            );

            lvClients.MouseDown += LvClients_MouseDown;

            tabClients.Controls.Add(lvClients);

            // =====================================================
            // GROUP TAB
            // =====================================================

            tabGroups = new TabPage(
                "Groups"
            );

            lvGroups = new ListView
            {
                Dock = DockStyle.Fill,

                View = View.Details,

                FullRowSelect = true,

                GridLines = true,

                ContextMenuStrip = cmsGroups,

                HideSelection = false
            };

            lvGroups.Columns.Add(
                "Tên nhóm",
                180
            );

            lvGroups.Columns.Add(
                "Chủ nhóm",
                130
            );

            lvGroups.Columns.Add(
                "Số thành viên",
                100
            );

            lvGroups.MouseDown += LvGroups_MouseDown;

            tabGroups.Controls.Add(lvGroups);

            // -----------------------------------------------------
            // ADD TABS
            // -----------------------------------------------------

            tabMain.TabPages.Add(tabClients);
            tabMain.TabPages.Add(tabGroups);

            // =====================================================
            // LOG PANEL
            // =====================================================

            // Giữ log bên phải.
            // 330px giúp phần log không quá hẹp.
            pnlLog = new Panel
            {
                Dock = DockStyle.Right,

                Width = 330,

                Padding = new Padding(6)
            };

            // -----------------------------------------------------
            // LOG TITLE
            // -----------------------------------------------------

            lblLogTitle = new Label
            {
                Text = "Nhật ký hoạt động (Log)",

                Dock = DockStyle.Top,

                Height = 28,

                TextAlign = ContentAlignment.MiddleCenter,

                Font = new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold
                )
            };

            // =====================================================
            // LOG BUTTON PANEL
            // =====================================================

            pnlLogButtons = new Panel
            {
                Dock = DockStyle.Bottom,

                Height = 42
            };

            // Xóa Log
            btnClearLog = new Button
            {
                Text = "Xóa Log",

                Location = new Point(0, 7),

                Size = new Size(88, 28)
            };

            btnClearLog.Click += BtnClearLog_Click;

            // Lưu Log
            btnSaveLog = new Button
            {
                Text = "Lưu",

                Location = new Point(96, 7),

                Size = new Size(88, 28)
            };

            btnSaveLog.Click += BtnSaveLog_Click;

            pnlLogButtons.Controls.Add(
                btnClearLog
            );

            pnlLogButtons.Controls.Add(
                btnSaveLog
            );

            // =====================================================
            // RICH TEXT LOG
            // =====================================================

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,

                ReadOnly = true,

                BackColor = Color.Black,

                ForeColor = Color.LightGreen,

                Font = new Font(
                    "Consolas",
                    9F,
                    FontStyle.Regular
                ),

                BorderStyle = BorderStyle.FixedSingle,

                WordWrap = false,

                ScrollBars = RichTextBoxScrollBars.Both
            };

            // -----------------------------------------------------
            // ADD LOG CONTROLS
            // -----------------------------------------------------

            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(pnlLogButtons);
            pnlLog.Controls.Add(lblLogTitle);

            // =====================================================
            // STATUS STRIP
            // =====================================================

            statusStrip = new StatusStrip();

            lblServerStatus =
                new ToolStripStatusLabel(
                    "● Server: Offline"
                )
                {
                    ForeColor = Color.Red
                };

            lblClientCount =
                new ToolStripStatusLabel(
                    "Clients online: 0"
                )
                {
                    Spring = false
                };

            lblGroupCount =
                new ToolStripStatusLabel(
                    "Groups: 0"
                )
                {
                    Spring = false
                };

            var spacer =
                new ToolStripStatusLabel
                {
                    Spring = true
                };

            statusStrip.Items.AddRange(
                new ToolStripItem[]
                {
                    lblServerStatus,
                    spacer,
                    lblClientCount,
                    lblGroupCount
                }
            );

            // =====================================================
            // ADD CONTROLS TO FORM
            // =====================================================

            // Thứ tự Add quan trọng với Dock.
            this.Controls.Add(tabMain);
            this.Controls.Add(pnlLog);
            this.Controls.Add(pnlTop);
            this.Controls.Add(statusStrip);

            this.FormClosing += ServerForm_FormClosing;
        }

        // =========================================================
        // BACKEND INITIALIZATION
        // =========================================================

        private void InitializeServerComponents()
        {
            _databaseService = new DatabaseService();

            _clientManager = new ClientManager();

            _groupManager = new GroupManager();

            _messageHandler =
                new MessageHandler(
                    _databaseService,
                    _clientManager,
                    _groupManager
                );

            _tcpServer = new TcpServer();

            _tcpServer.ClientConnected +=
                TcpServer_ClientConnected;

            _tcpServer.MessageReceived +=
                TcpServer_MessageReceived;

            _groupManager.GroupCreated +=
                GroupManager_GroupCreated;

            _groupManager.GroupUpdated +=
                GroupManager_GroupUpdated;

            _groupManager.GroupDissolved +=
                GroupManager_GroupDissolved;

            _logger = Logger.Instance;

            AppendLog(
                "[SYSTEM] Server Console đã khởi tạo. Sẵn sàng để Start."
            );
        }

        // =========================================================
        // TCP SERVER EVENTS
        // =========================================================

        private void TcpServer_ClientConnected(
            ClientConnection connection)
        {
            AppendLog(
                "[CONNECT] Một Client mới đã kết nối tới Server."
            );

            connection.Disconnected +=
                TcpServer_ClientDisconnected;
        }

        private void TcpServer_ClientDisconnected(
            ClientConnection connection)
        {
            _clientManager?.RemoveClient(connection);

            InvokeIfRequired(() =>
            {
                if (
                    connection.UserId > 0 &&
                    !string.IsNullOrEmpty(
                        connection.Username
                    )
                )
                {
                    RemoveClientFromList(
                        connection.Username
                    );

                    AppendLog(
                        $"[DISCONNECT] \"{connection.Username}\" đã ngắt kết nối."
                    );
                }
                else
                {
                    AppendLog(
                        "[DISCONNECT] Một Client đã ngắt kết nối (chưa đăng nhập)."
                    );
                }
            });
        }

        private void TcpServer_MessageReceived(
            ClientConnection connection,
            Message message)
        {
            AppendLog(
                $"[MSG] Nhận {message.Type} từ Client."
            );

            _messageHandler?.HandleIncomingMessage(
                message,
                connection
            );

            if (
                message.Type ==
                ChatTCP.Shared.Enums.MessageType.LoginRequest
                &&
                connection.UserId > 0
                &&
                !string.IsNullOrEmpty(
                    connection.Username
                )
            )
            {
                OnClientConnected(
                    connection.Username,
                    GetRemoteIp(connection)
                );
            }
        }

        private static string GetRemoteIp(
            ClientConnection connection)
        {
            return connection.RemoteIp;
        }

        // =========================================================
        // GROUP EVENTS
        // =========================================================

        private void GroupManager_GroupCreated(
            Group group)
        {
            InvokeIfRequired(() =>
            {
                AddOrUpdateGroupInList(group);

                AppendLog(
                    $"[GROUP] Nhóm \"{group.GroupName}\" được tạo bởi " +
                    $"\"{ResolveUsername(group.CreatedBy)}\" " +
                    $"({group.MemberIds.Count} thành viên)."
                );
            });
        }

        private void GroupManager_GroupUpdated(
            Group group)
        {
            InvokeIfRequired(() =>
            {
                AddOrUpdateGroupInList(group);

                AppendLog(
                    $"[GROUP] Nhóm \"{group.GroupName}\" vừa được cập nhật " +
                    $"({group.MemberIds.Count} thành viên)."
                );
            });
        }

        private void GroupManager_GroupDissolved(
            Group group)
        {
            InvokeIfRequired(() =>
            {
                RemoveGroupFromList(
                    group.GroupId
                );

                AppendLog(
                    $"[GROUP] Nhóm \"{group.GroupName}\" đã bị giải tán."
                );
            });
        }

        // =========================================================
        // RESOLVE USERNAME
        // =========================================================

        private string ResolveUsername(int userId)
        {
            var onlineClient =
                _clientManager?.GetClient(userId);

            if (
                !string.IsNullOrEmpty(
                    onlineClient?.Username
                )
            )
            {
                return onlineClient!.Username;
            }

            var dbUser =
                _databaseService?.GetUserById(userId);

            if (
                !string.IsNullOrEmpty(
                    dbUser?.Username
                )
            )
            {
                return dbUser!.Username;
            }

            return $"UserId #{userId}";
        }

        // =========================================================
        // START SERVER
        // =========================================================

        private void BtnStart_Click(
            object? sender,
            EventArgs e)
        {
            int port = (int)numPort.Value;

            try
            {
                bool started =
                    _tcpServer != null &&
                    _tcpServer.Start(port);

                if (!started)
                {
                    AppendLog(
                        $"[ERROR] Không thể khởi động Server tại cổng {port}."
                    );

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

                btnStart.Enabled = false;
                btnStop.Enabled = true;

                numPort.Enabled = false;
                btnRefreshIp.Enabled = false;

                lblServerStatus.Text =
                    "● Server: Online";

                lblServerStatus.ForeColor =
                    Color.LimeGreen;

                AppendLog(
                    $"[SYSTEM] Server đã khởi động tại {txtIp.Text}:{port}"
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "BtnStart_Click"
                );

                AppendLog(
                    $"[ERROR] Không thể khởi động Server: {ex.Message}"
                );

                MessageBox.Show(
                    $"Không thể khởi động Server:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // =========================================================
        // STOP SERVER
        // =========================================================

        private void BtnStop_Click(
            object? sender,
            EventArgs e)
        {
            _tcpServer?.Stop();

            _isRunning = false;

            btnStart.Enabled = true;
            btnStop.Enabled = false;

            numPort.Enabled = true;
            btnRefreshIp.Enabled = true;

            lblServerStatus.Text =
                "● Server: Offline";

            lblServerStatus.ForeColor =
                Color.Red;

            lvClients.Items.Clear();

            _clientItems.Clear();

            UpdateClientCount(0);

            AppendLog(
                "[SYSTEM] Server đã dừng."
            );
        }

        // =========================================================
        // REFRESH IP
        // =========================================================

        private void BtnRefreshIp_Click(
            object? sender,
            EventArgs e)
        {
            txtIp.Text =
                GetLocalIPAddress();

            AppendLog(
                "[SYSTEM] Đã làm mới địa chỉ IP."
            );
        }

        // =========================================================
        // FORM CLOSING
        // =========================================================

        private void ServerForm_FormClosing(
            object? sender,
            FormClosingEventArgs e)
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

        // =========================================================
        // CLIENT LIST
        // =========================================================

        private void LvClients_MouseDown(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item =
                    lvClients.GetItemAt(
                        e.X,
                        e.Y
                    );

                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }

        private void MiViewClientInfo_Click(
            object? sender,
            EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0)
                return;

            var item =
                lvClients.SelectedItems[0];

            string username =
                item.SubItems[0].Text;

            string ip =
                item.SubItems[1].Text;

            string status =
                item.SubItems[2].Text;

            string connectedAt =
                item.SubItems[3].Text;

            MessageBox.Show(
                $"Username: {username}\n" +
                $"IP: {ip}\n" +
                $"Trạng thái: {status}\n" +
                $"Kết nối lúc: {connectedAt}",
                "Thông tin Client",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void MiKickClient_Click(
            object? sender,
            EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0)
                return;

            var item =
                lvClients.SelectedItems[0];

            string username =
                item.SubItems[0].Text;

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn ngắt kết nối \"{username}\"?",
                "Xác nhận Kick",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            _clientManager?.DisconnectClient(
                username
            );

            AppendLog(
                $"[SYSTEM] Đã gửi yêu cầu ngắt kết nối \"{username}\" (Kick bởi Admin)."
            );
        }

        // =========================================================
        // GROUP LIST
        // =========================================================

        private void LvGroups_MouseDown(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item =
                    lvGroups.GetItemAt(
                        e.X,
                        e.Y
                    );

                if (item != null)
                {
                    item.Selected = true;
                }
            }
        }

        private void MiViewGroupMembers_Click(
            object? sender,
            EventArgs e)
        {
            if (lvGroups.SelectedItems.Count == 0)
                return;

            var item =
                lvGroups.SelectedItems[0];

            string groupName =
                item.SubItems[0].Text;

            if (
                item.Tag is not int groupId ||
                _groupManager == null
            )
            {
                MessageBox.Show(
                    "Không xác định được nhóm đã chọn.",
                    "Thành viên nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            Group? group =
                _groupManager.GetGroupById(
                    groupId
                );

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

            string memberList =
                string.Join(
                    "\n",
                    group.MemberIds.Select(
                        id =>
                            id == group.CreatedBy
                                ? $"{ResolveUsername(id)} (Trưởng nhóm)"
                                : ResolveUsername(id)
                    )
                );

            MessageBox.Show(
                $"Nhóm \"{group.GroupName}\" - " +
                $"{group.MemberIds.Count} thành viên:\n\n" +
                memberList,
                "Thành viên nhóm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // =========================================================
        // CLIENT CONNECTED
        // =========================================================

        public void OnClientConnected(
            string username,
            string ipAddress)
        {
            InvokeIfRequired(() =>
            {
                AddClientToList(
                    username,
                    ipAddress,
                    "Online",
                    DateTime.Now.ToString(
                        "HH:mm:ss dd/MM"
                    )
                );

                AppendLog(
                    $"[CONNECT] \"{username}\" ({ipAddress}) đã kết nối."
                );
            });
        }

        // =========================================================
        // ADD CLIENT
        // =========================================================

        private void AddClientToList(
            string username,
            string ip,
            string status,
            string connectedAt)
        {
            if (_clientItems.ContainsKey(username))
            {
                UpdateClientStatus(
                    username,
                    status
                );

                return;
            }

            var item =
                new ListViewItem(
                    new[]
                    {
                        username,
                        ip,
                        status,
                        connectedAt
                    }
                );

            item.SubItems[2].ForeColor =
                status == "Online"
                    ? Color.SeaGreen
                    : Color.Gray;

            lvClients.Items.Add(item);

            _clientItems[username] =
                item;

            UpdateClientCount(
                _clientItems.Count
            );
        }

        // =========================================================
        // REMOVE CLIENT
        // =========================================================

        private void RemoveClientFromList(
            string username)
        {
            if (
                _clientItems.TryGetValue(
                    username,
                    out var item
                )
            )
            {
                lvClients.Items.Remove(item);

                _clientItems.Remove(username);

                UpdateClientCount(
                    _clientItems.Count
                );
            }
        }

        // =========================================================
        // UPDATE CLIENT STATUS
        // =========================================================

        private void UpdateClientStatus(
            string username,
            string status)
        {
            if (
                _clientItems.TryGetValue(
                    username,
                    out var item
                )
            )
            {
                item.SubItems[2].Text =
                    status;

                item.SubItems[2].ForeColor =
                    status == "Online"
                        ? Color.SeaGreen
                        : Color.Gray;
            }
        }

        // =========================================================
        // ADD / UPDATE GROUP
        // =========================================================

        private void AddOrUpdateGroupInList(
            Group group)
        {
            string ownerName =
                ResolveUsername(
                    group.CreatedBy
                );

            if (
                _groupItems.TryGetValue(
                    group.GroupId,
                    out var existing
                )
            )
            {
                existing.SubItems[0].Text =
                    group.GroupName;

                existing.SubItems[1].Text =
                    ownerName;

                existing.SubItems[2].Text =
                    group.MemberIds.Count.ToString();

                return;
            }

            var item =
                new ListViewItem(
                    new[]
                    {
                        group.GroupName,
                        ownerName,
                        group.MemberIds.Count.ToString()
                    }
                )
                {
                    Tag = group.GroupId
                };

            lvGroups.Items.Add(item);

            _groupItems[group.GroupId] =
                item;

            lblGroupCount.Text =
                $"Groups: {_groupItems.Count}";
        }

        // =========================================================
        // REMOVE GROUP
        // =========================================================

        private void RemoveGroupFromList(
            int groupId)
        {
            if (
                _groupItems.TryGetValue(
                    groupId,
                    out var item
                )
            )
            {
                lvGroups.Items.Remove(item);

                _groupItems.Remove(groupId);

                lblGroupCount.Text =
                    $"Groups: {_groupItems.Count}";
            }
        }

        // =========================================================
        // LOG
        // =========================================================

        private void AppendLog(
            string message)
        {
            InvokeIfRequired(() =>
            {
                Color color =
                    Color.LightGreen;

                if (message.Contains("[ERROR]"))
                {
                    color = Color.IndianRed;
                }
                else if (
                    message.Contains("[DISCONNECT]")
                )
                {
                    color = Color.Orange;
                }
                else if (
                    message.Contains("[SYSTEM]")
                )
                {
                    color = Color.LightBlue;
                }

                rtbLog.SelectionStart =
                    rtbLog.TextLength;

                rtbLog.SelectionLength = 0;

                rtbLog.SelectionColor =
                    color;

                rtbLog.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] {message}\n"
                );

                rtbLog.SelectionColor =
                    rtbLog.ForeColor;

                rtbLog.ScrollToCaret();
            });

            _logger?.Log(message);
        }

        // =========================================================
        // CLEAR LOG
        // =========================================================

        private void BtnClearLog_Click(
            object? sender,
            EventArgs e)
        {
            rtbLog.Clear();
        }

        // =========================================================
        // SAVE LOG
        // =========================================================

        private void BtnSaveLog_Click(
            object? sender,
            EventArgs e)
        {
            using (
                var sfd =
                    new SaveFileDialog
                    {
                        Filter =
                            "Text file (*.txt)|*.txt",

                        FileName =
                            $"ServerLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                    }
            )
            {
                if (
                    sfd.ShowDialog() ==
                    DialogResult.OK
                )
                {
                    try
                    {
                        System.IO.File.WriteAllText(
                            sfd.FileName,
                            rtbLog.Text
                        );

                        MessageBox.Show(
                            "Đã lưu Log thành công.",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            ex,
                            "BtnSaveLog_Click"
                        );

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

        // =========================================================
        // CLIENT COUNT
        // =========================================================

        private void UpdateClientCount(
            int count)
        {
            lblClientCount.Text =
                $"Clients online: {count}";
        }

        // =========================================================
        // GET LOCAL IP
        // =========================================================

        private static string GetLocalIPAddress()
        {
            try
            {
                var host =
                    Dns.GetHostEntry(
                        Dns.GetHostName()
                    );

                foreach (
                    var ip in host.AddressList
                )
                {
                    if (
                        ip.AddressFamily ==
                        AddressFamily.InterNetwork
                    )
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Nếu không lấy được IP
                // dùng loopback.
            }

            return "127.0.0.1";
        }

        // =========================================================
        // INVOKE UI SAFELY
        // =========================================================

        private void InvokeIfRequired(
            Action action)
        {
            if (this.IsDisposed ||
                this.Disposing)
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
                    // Form đang đóng.
                }
            }
            else
            {
                action();
            }
        }
    }
}