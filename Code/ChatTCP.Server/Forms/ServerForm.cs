using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// Tạm thời comment các thư viện backend chưa có code
// using ChatTCP.Server.Network;
// using ChatTCP.Server.Services;
// using ChatTCP.Server.Utils;
// using ChatTCP.Shared.Models;

namespace ChatTCP.Server.Forms
{
    public partial class ServerForm : Form
    {
        // Tạm thời ẩn các biến backend
        // private TcpServer _tcpServer;
        // private ClientManager _clientManager;
        // private GroupManager _groupManager;
        // private Logger _logger;

        private bool _isRunning = false;

        // ==== UI Controls ====
        private Panel pnlTop;
        private Label lblIp;
        private TextBox txtIp;
        private Label lblPort;
        private NumericUpDown numPort;
        private Button btnStart;
        private Button btnStop;

        private TabControl tabMain;
        private TabPage tabClients;
        private TabPage tabGroups;

        private ListView lvClients;
        private ListView lvGroups;

        private RichTextBox rtbLog;
        private Label lblLogTitle;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblServerStatus;
        private ToolStripStatusLabel lblClientCount;

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
            txtIp = new TextBox { Location = new Point(35, 14), Width = 110, Text = "127.0.0.1", ReadOnly = true };

            lblPort = new Label { Text = "Port:", Location = new Point(160, 18), AutoSize = true };
            numPort = new NumericUpDown
            {
                Location = new Point(200, 14),
                Width = 80,
                Minimum = 1024,
                Maximum = 65535,
                Value = 5000
            };

            btnStart = new Button
            {
                Text = "Start Server",
                Location = new Point(300, 12),
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
                Location = new Point(420, 12),
                Width = 110,
                Height = 30,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            pnlTop.Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, numPort, btnStart, btnStop });

            // ---- Tab control: Clients / Groups ----
            tabMain = new TabControl { Dock = DockStyle.Fill };

            tabClients = new TabPage("Clients Online");
            lvClients = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvClients.Columns.Add("Username", 150);
            lvClients.Columns.Add("IP Address", 130);
            lvClients.Columns.Add("Trạng thái", 100);
            lvClients.Columns.Add("Thời gian kết nối", 150);
            tabClients.Controls.Add(lvClients);

            tabGroups = new TabPage("Groups");
            lvGroups = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvGroups.Columns.Add("Tên nhóm", 180);
            lvGroups.Columns.Add("Chủ nhóm", 130);
            lvGroups.Columns.Add("Số thành viên", 100);
            tabGroups.Controls.Add(lvGroups);

            tabMain.TabPages.Add(tabClients);
            tabMain.TabPages.Add(tabGroups);

            // ---- Log panel (bên phải) ----
            var pnlLog = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(5) };
            lblLogTitle = new Label { Text = "Nhật ký hoạt động (Log)", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F)
            };
            pnlLog.Controls.Add(rtbLog);
            pnlLog.Controls.Add(lblLogTitle);

            // ---- Status strip ----
            statusStrip = new StatusStrip();
            lblServerStatus = new ToolStripStatusLabel("● Server: Offline") { ForeColor = Color.Red };
            lblClientCount = new ToolStripStatusLabel("Clients online: 0") { Spring = false };
            var spacer = new ToolStripStatusLabel { Spring = true };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblServerStatus, spacer, lblClientCount });

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
            // Tạm thời để trống để xem trước Giao diện (Demo UI)
        }

        // ================== EVENT HANDLERS ==================
        private void BtnStart_Click(object sender, EventArgs e)
        {
            int port = (int)numPort.Value;
            _isRunning = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            numPort.Enabled = false;

            lblServerStatus.Text = "● Server: Online";
            lblServerStatus.ForeColor = Color.LimeGreen;

            AppendLog($"[SYSTEM] Server đã khởi động tại {txtIp.Text}:{port}");
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _isRunning = false;

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            numPort.Enabled = true;

            lblServerStatus.Text = "● Server: Offline";
            lblServerStatus.ForeColor = Color.Red;

            lvClients.Items.Clear();
            UpdateClientCount(0);

            AppendLog("[SYSTEM] Server đã dừng.");
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
                }
            }
        }

        // ================== HELPERS ==================
        private void AppendLog(string message)
        {
            InvokeIfRequired(() =>
            {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                rtbLog.ScrollToCaret();
            });
        }

        private void UpdateClientCount(int count)
        {
            lblClientCount.Text = $"Clients online: {count}";
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