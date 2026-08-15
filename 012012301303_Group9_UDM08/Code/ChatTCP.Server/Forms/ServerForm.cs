using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

// Tạm thời comment các thư viện backend chưa có code
// Khi các thành viên khác hoàn thành, bỏ comment và nối vào các điểm TODO bên dưới
// using ChatTCP.Server.Network;
// using ChatTCP.Server.Services;
// using ChatTCP.Server.Utils;
// using ChatTCP.Shared.Models;
// using ChatTCP.Shared.Enums;

namespace ChatTCP.Server.Forms
{
    public partial class ServerForm : Form
    {
        // Tạm thời ẩn các biến backend, sẽ khai báo thật khi có code từ các TV khác
        // private TcpServer _tcpServer;           // [TV6]
        // private ClientManager _clientManager;    // [TV5]
        // private GroupManager _groupManager;       // [TV3]
        // private MessageHandler _messageHandler;   // [TV4]
        // private Logger _logger;                   // [TV6]

        private bool _isRunning = false;

        // ==== UI Controls ====
        private Panel pnlTop;
        private Label lblIp;
        private TextBox txtIp;
        private Label lblPort;
        private NumericUpDown numPort;
        private Button btnStart;
        private Button btnStop;
        private Button btnRefreshIp;

        private TabControl tabMain;
        private TabPage tabClients;
        private TabPage tabGroups;

        private ListView lvClients;
        private ListView lvGroups;
        private ContextMenuStrip cmsClients;
        private ToolStripMenuItem miKickClient;
        private ToolStripMenuItem miViewClientInfo;

        private ContextMenuStrip cmsGroups;
        private ToolStripMenuItem miViewGroupMembers;

        private Panel pnlLog;
        private Label lblLogTitle;
        private RichTextBox rtbLog;
        private Panel pnlLogButtons;
        private Button btnClearLog;
        private Button btnSaveLog;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblServerStatus;
        private ToolStripStatusLabel lblClientCount;
        private ToolStripStatusLabel lblGroupCount;

        // Lưu danh sách Client đang hiển thị (mô phỏng, sẽ thay bằng ClientManager thật)
        private readonly Dictionary<string, ListViewItem> _clientItems = new Dictionary<string, ListViewItem>();
        private readonly Dictionary<string, ListViewItem> _groupItems = new Dictionary<string, ListViewItem>();

        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        // ================== UI SETUP ==================
        private void InitializeComponent()
        {
            this.Text = "ChatTCP - Server Console";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);

            // ---- Top panel: cấu hình + điều khiển server ----
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(10) };

            lblIp = new Label { Text = "IP:", Location = new Point(10, 18), AutoSize = true };
            txtIp = new TextBox { Location = new Point(35, 14), Width = 110, Text = GetLocalIPAddress(), ReadOnly = true };

            btnRefreshIp = new Button
            {
                Text = "⟳",
                Location = new Point(148, 13),
                Width = 28,
                Height = 24,
                FlatStyle = FlatStyle.Flat
            };
            btnRefreshIp.Click += BtnRefreshIp_Click;

            lblPort = new Label { Text = "Port:", Location = new Point(188, 18), AutoSize = true };
            numPort = new NumericUpDown
            {
                Location = new Point(228, 14),
                Width = 80,
                Minimum = 1024,
                Maximum = 65535,
                Value = 5000
            };

            btnStart = new Button
            {
                Text = "Start Server",
                Location = new Point(325, 12),
                Width = 110,
                Height = 30,
                BackColor = Color.MediumSeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "Stop Server",
                Location = new Point(445, 12),
                Width = 110,
                Height = 30,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            pnlTop.Controls.AddRange(new Control[] { lblIp, txtIp, btnRefreshIp, lblPort, numPort, btnStart, btnStop });

            // ---- Context menu cho danh sách Client ----
            cmsClients = new ContextMenuStrip();
            miViewClientInfo = new ToolStripMenuItem("Xem thông tin");
            miViewClientInfo.Click += MiViewClientInfo_Click;
            miKickClient = new ToolStripMenuItem("Ngắt kết nối (Kick)");
            miKickClient.Click += MiKickClient_Click;
            cmsClients.Items.Add(miViewClientInfo);
            cmsClients.Items.Add(miKickClient);

            // ---- Context menu cho danh sách Group ----
            cmsGroups = new ContextMenuStrip();
            miViewGroupMembers = new ToolStripMenuItem("Xem thành viên nhóm");
            miViewGroupMembers.Click += MiViewGroupMembers_Click;
            cmsGroups.Items.Add(miViewGroupMembers);

            // ---- Tab control: Clients / Groups ----
            tabMain = new TabControl { Dock = DockStyle.Fill };

            tabClients = new TabPage("Clients Online");
            lvClients = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                ContextMenuStrip = cmsClients
            };
            lvClients.Columns.Add("Username", 150);
            lvClients.Columns.Add("IP Address", 130);
            lvClients.Columns.Add("Trạng thái", 100);
            lvClients.Columns.Add("Thời gian kết nối", 150);
            lvClients.MouseDown += LvClients_MouseDown;
            tabClients.Controls.Add(lvClients);

            tabGroups = new TabPage("Groups");
            lvGroups = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                ContextMenuStrip = cmsGroups
            };
            lvGroups.Columns.Add("Tên nhóm", 180);
            lvGroups.Columns.Add("Chủ nhóm", 130);
            lvGroups.Columns.Add("Số thành viên", 100);
            lvGroups.MouseDown += LvGroups_MouseDown;
            tabGroups.Controls.Add(lvGroups);

            tabMain.TabPages.Add(tabClients);
            tabMain.TabPages.Add(tabGroups);

            // ---- Log panel (bên phải) ----
            pnlLog = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(5) };
            lblLogTitle = new Label
            {
                Text = "Nhật ký hoạt động (Log)",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            pnlLogButtons = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnClearLog = new Button { Text = "Xóa Log", Width = 90, Height = 26, Location = new Point(0, 3) };
            btnClearLog.Click += BtnClearLog_Click;
            btnSaveLog = new Button { Text = "Lưu Log...", Width = 90, Height = 26, Location = new Point(100, 3) };
            btnSaveLog.Click += BtnSaveLog_Click;
            pnlLogButtons.Controls.AddRange(new Control[] { btnClearLog, btnSaveLog });

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F)
            };

            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(pnlLogButtons);
            pnlLog.Controls.Add(lblLogTitle);

            // ---- Status strip ----
            statusStrip = new StatusStrip();
            lblServerStatus = new ToolStripStatusLabel("● Server: Offline") { ForeColor = Color.Red };
            lblClientCount = new ToolStripStatusLabel("Clients online: 0") { Spring = false };
            lblGroupCount = new ToolStripStatusLabel("Groups: 0") { Spring = false };
            var spacer = new ToolStripStatusLabel { Spring = true };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblServerStatus, spacer, lblClientCount, lblGroupCount });

            // ---- Add to form ----
            this.Controls.Add(tabMain);
            this.Controls.Add(pnlLog);
            this.Controls.Add(pnlTop);
            this.Controls.Add(statusStrip);

            this.FormClosing += ServerForm_FormClosing;
        }

        // ================== LOGIC SETUP ==================
        private void InitializeServerComponents()
        {
            // TODO: [TV6] Khởi tạo TcpServer khi lớp TcpServer.cs đã sẵn sàng
            // _tcpServer = new TcpServer();
            // _tcpServer.OnClientConnected += TcpServer_OnClientConnected;
            // _tcpServer.OnClientDisconnected += TcpServer_OnClientDisconnected;
            // _tcpServer.OnMessageReceived += TcpServer_OnMessageReceived;
            // _tcpServer.OnLog += (msg) => AppendLog(msg);

            // TODO: [TV5] Khởi tạo ClientManager khi lớp ClientManager.cs đã sẵn sàng
            // _clientManager = new ClientManager();

            // TODO: [TV3] Khởi tạo GroupManager khi lớp GroupManager.cs đã sẵn sàng
            // _groupManager = new GroupManager();

            // TODO: [TV4] Khởi tạo MessageHandler khi lớp MessageHandler.cs đã sẵn sàng
            // _messageHandler = new MessageHandler(_clientManager, _groupManager);

            // TODO: [TV6] Khởi tạo Logger khi lớp Logger.cs đã sẵn sàng
            // _logger = new Logger();

            AppendLog("[SYSTEM] Server Console đã khởi tạo. Sẵn sàng để Start.");
        }

        // ================== EVENT HANDLERS - ĐIỀU KHIỂN SERVER ==================
        private void BtnStart_Click(object sender, EventArgs e)
        {
            int port = (int)numPort.Value;

            try
            {
                // TODO: [TV6] Gọi hàm khởi động thật của TcpServer
                // _tcpServer.Start(txtIp.Text, port);

                _isRunning = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                numPort.Enabled = false;
                btnRefreshIp.Enabled = false;

                lblServerStatus.Text = "● Server: Online";
                lblServerStatus.ForeColor = Color.LimeGreen;

                AppendLog($"[SYSTEM] Server đã khởi động tại {txtIp.Text}:{port}");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Không thể khởi động Server: {ex.Message}");
                MessageBox.Show($"Không thể khởi động Server:\n{ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            // TODO: [TV6] Gọi hàm dừng thật của TcpServer (đóng Socket, ngắt hết Client)
            // _tcpServer.Stop();

            _isRunning = false;

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            numPort.Enabled = true;
            btnRefreshIp.Enabled = true;

            lblServerStatus.Text = "● Server: Offline";
            lblServerStatus.ForeColor = Color.Red;

            lvClients.Items.Clear();
            _clientItems.Clear();
            UpdateClientCount(0);

            AppendLog("[SYSTEM] Server đã dừng.");
        }

        private void BtnRefreshIp_Click(object sender, EventArgs e)
        {
            txtIp.Text = GetLocalIPAddress();
            AppendLog("[SYSTEM] Đã làm mới địa chỉ IP.");
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRunning)
            {
                var result = MessageBox.Show(
                    "Server đang chạy. Bạn có chắc muốn thoát?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // TODO: [TV6] Dừng TcpServer trước khi thoát để đóng kết nối an toàn
                // _tcpServer?.Stop();
            }
        }

        // ================== EVENT HANDLERS - CLIENT LIST ==================
        private void LvClients_MouseDown(object sender, MouseEventArgs e)
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

        private void MiViewClientInfo_Click(object sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var item = lvClients.SelectedItems[0];
            string username = item.SubItems[0].Text;
            string ip = item.SubItems[1].Text;
            string status = item.SubItems[2].Text;
            string connectedAt = item.SubItems[3].Text;

            MessageBox.Show(
                $"Username: {username}\nIP: {ip}\nTrạng thái: {status}\nKết nối lúc: {connectedAt}",
                "Thông tin Client",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void MiKickClient_Click(object sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var item = lvClients.SelectedItems[0];
            string username = item.SubItems[0].Text;

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn ngắt kết nối \"{username}\"?",
                "Xác nhận Kick",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            // TODO: [TV5] Gọi ClientManager để ngắt kết nối Client thật
            // _clientManager.DisconnectClient(username);

            RemoveClientFromList(username);
            AppendLog($"[SYSTEM] Đã ngắt kết nối Client \"{username}\" (Kick bởi Admin).");
        }

        // ================== EVENT HANDLERS - GROUP LIST ==================
        private void LvGroups_MouseDown(object sender, MouseEventArgs e)
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

        private void MiViewGroupMembers_Click(object sender, EventArgs e)
        {
            if (lvGroups.SelectedItems.Count == 0) return;

            var item = lvGroups.SelectedItems[0];
            string groupName = item.SubItems[0].Text;

            // TODO: [TV3] Lấy danh sách thành viên thật từ GroupManager
            // var members = _groupManager.GetMembers(groupName);
            // string memberList = string.Join("\n", members);

            MessageBox.Show(
                $"Chức năng xem thành viên nhóm \"{groupName}\" sẽ hoạt động khi GroupManager.cs được tích hợp.",
                "Thành viên nhóm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ================== HOOKS TÍCH HỢP BACKEND (dành cho TcpServer, ClientManager...) ==================
        // Các hàm dưới đây được gọi từ backend (qua sự kiện) để cập nhật UI.
        // Khi tích hợp thật, nối các sự kiện của TcpServer/ClientManager/GroupManager vào đây.

        /// <summary>
        /// Gọi khi có Client mới kết nối thành công (đăng nhập). [Hook cho TV5/TV6]
        /// </summary>
        public void OnClientConnected(string username, string ipAddress)
        {
            InvokeIfRequired(() =>
            {
                AddClientToList(username, ipAddress, "Online", DateTime.Now.ToString("HH:mm:ss dd/MM"));
                AppendLog($"[CONNECT] \"{username}\" ({ipAddress}) đã kết nối.");
            });
        }

        /// <summary>
        /// Gọi khi Client ngắt kết nối (chủ động thoát hoặc mất kết nối). [Hook cho TV5/TV6]
        /// </summary>
        public void OnClientDisconnected(string username)
        {
            InvokeIfRequired(() =>
            {
                RemoveClientFromList(username);
                AppendLog($"[DISCONNECT] \"{username}\" đã ngắt kết nối.");
            });
        }

        /// <summary>
        /// Gọi khi có nhóm chat mới được tạo. [Hook cho TV3]
        /// </summary>
        public void OnGroupCreated(string groupName, string owner, int memberCount)
        {
            InvokeIfRequired(() =>
            {
                AddGroupToList(groupName, owner, memberCount);
                AppendLog($"[GROUP] Nhóm \"{groupName}\" được tạo bởi \"{owner}\" ({memberCount} thành viên).");
            });
        }

        /// <summary>
        /// Gọi khi có tin nhắn đi qua Server (dùng để log / thống kê). [Hook cho TV2/TV4]
        /// </summary>
        public void OnMessageRelayed(string from, string to, string messageType)
        {
            InvokeIfRequired(() =>
            {
                AppendLog($"[MSG] {messageType} | {from} → {to}");
            });
        }

        // ================== HELPERS - CLIENT/GROUP LIST ==================
        private void AddClientToList(string username, string ip, string status, string connectedAt)
        {
            if (_clientItems.ContainsKey(username))
            {
                // Đã tồn tại -> cập nhật trạng thái thay vì thêm trùng
                UpdateClientStatus(username, status);
                return;
            }

            var item = new ListViewItem(new[] { username, ip, status, connectedAt });
            item.SubItems[2].ForeColor = status == "Online" ? Color.SeaGreen : Color.Gray;

            lvClients.Items.Add(item);
            _clientItems[username] = item;

            UpdateClientCount(_clientItems.Count);
        }

        private void RemoveClientFromList(string username)
        {
            if (_clientItems.TryGetValue(username, out var item))
            {
                lvClients.Items.Remove(item);
                _clientItems.Remove(username);
                UpdateClientCount(_clientItems.Count);
            }
        }

        private void UpdateClientStatus(string username, string status)
        {
            if (_clientItems.TryGetValue(username, out var item))
            {
                item.SubItems[2].Text = status;
                item.SubItems[2].ForeColor = status == "Online" ? Color.SeaGreen : Color.Gray;
            }
        }

        private void AddGroupToList(string groupName, string owner, int memberCount)
        {
            if (_groupItems.ContainsKey(groupName))
            {
                _groupItems[groupName].SubItems[2].Text = memberCount.ToString();
                return;
            }

            var item = new ListViewItem(new[] { groupName, owner, memberCount.ToString() });
            lvGroups.Items.Add(item);
            _groupItems[groupName] = item;

            lblGroupCount.Text = $"Groups: {_groupItems.Count}";
        }

        // ================== HELPERS - LOG ==================
        private void AppendLog(string message)
        {
            InvokeIfRequired(() =>
            {
                Color color = Color.LightGreen;
                if (message.Contains("[ERROR]")) color = Color.IndianRed;
                else if (message.Contains("[DISCONNECT]")) color = Color.Orange;
                else if (message.Contains("[SYSTEM]")) color = Color.LightBlue;

                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionLength = 0;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                rtbLog.SelectionColor = rtbLog.ForeColor;
                rtbLog.ScrollToCaret();

                // TODO: [TV6] Ghi song song vào file log thật qua Logger.cs
                // _logger.Log(message);
            });
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        private void BtnSaveLog_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt",
                FileName = $"ServerLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(sfd.FileName, rtbLog.Text);
                    MessageBox.Show("Đã lưu Log thành công.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateClientCount(int count)
        {
            lblClientCount.Text = $"Clients online: {count}";
        }

        // ================== HELPERS - KHÁC ==================
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
                // Nếu không lấy được IP thật, dùng IP loopback mặc định
            }

            return "127.0.0.1";
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