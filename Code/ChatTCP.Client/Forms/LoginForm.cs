using ChatTCP.Client.Services;
using ChatTCP.Shared.Models;
using ChatTCP.Shared.Utils;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ChatTCP.Client.Forms
{
    public class LoginForm : Form
    {
        // AUTH SERVICE
        private readonly AuthService authService;
        // CONTROLS
        private TextBox txtServerIp = null!;
        private NumericUpDown numServerPort = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkShowPassword = null!;
        private Button btnLogin = null!;
        private LinkLabel lnkRegister = null!;
        private Label lblStatus = null!;
        // USER
        public User? LoggedInUser { get; private set; }
        // CONSTRUCTOR
        public LoginForm(AuthService authService)
        {
            this.authService = authService;
            InitializeUi();
            this.authService.LoginSucceeded +=
                OnLoginSucceeded;
            this.authService.LoginFailed +=
                OnLoginFailed;
            FormClosed +=
                LoginForm_FormClosed;
        }
        // INITIALIZE UI
        private void InitializeUi()
        {
            // FORM
            Text = "Chat TCP - Đăng nhập";
            ClientSize = new Size(420, 560);
            StartPosition =
                FormStartPosition.CenterScreen;
            FormBorderStyle =
                FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            BackColor = Color.White;
            Font =
                new Font(
                    "Segoe UI",
                    9F);
            // TITLE
            Label lblTitle =
                new Label();
            lblTitle.Text = "Chat TCP";
            lblTitle.Location =
                new Point(
                    0,
                    18);
            lblTitle.Size =
                new Size(
                    420,
                    45);
            lblTitle.TextAlign =
                ContentAlignment.MiddleCenter;
            lblTitle.ForeColor =
                Color.FromArgb(
                    30,
                    71,
                    180);
            lblTitle.Font =
                new Font(
                    "Segoe UI",
                    20F,
                    FontStyle.Bold);
            // SUBTITLE
            Label lblSubtitle =
                new Label();
            lblSubtitle.Text =
                "Đăng nhập để bắt đầu trò chuyện";
            lblSubtitle.Location =
                new Point(
                    0,
                    65);
            lblSubtitle.Size =
                new Size(
                    420,
                    24);
            lblSubtitle.TextAlign =
                ContentAlignment.MiddleCenter;
            lblSubtitle.ForeColor =
                Color.Gray;
            lblSubtitle.Font =
                new Font(
                    "Segoe UI",
                    9F);

            // SERVER IP LABEL
            Label lblServerIp = new Label();
            lblServerIp.Text = "Địa chỉ Server (IP/Host):";
            lblServerIp.Location = new Point(45, 98);
            lblServerIp.Size = new Size(210, 20);
            lblServerIp.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblServerIp.ForeColor = Color.FromArgb(60, 60, 60);

            // SERVER IP TEXTBOX
            txtServerIp = new TextBox();
            txtServerIp.Location = new Point(45, 120);
            txtServerIp.Size = new Size(210, 27);
            txtServerIp.Font = new Font("Segoe UI", 9.5F);
            txtServerIp.Text = NetworkConfig.ServerIp;

            // SERVER PORT LABEL
            Label lblServerPort = new Label();
            lblServerPort.Text = "Port:";
            lblServerPort.Location = new Point(270, 98);
            lblServerPort.Size = new Size(105, 20);
            lblServerPort.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblServerPort.ForeColor = Color.FromArgb(60, 60, 60);

            // SERVER PORT NUMERIC
            numServerPort = new NumericUpDown();
            numServerPort.Location = new Point(270, 120);
            numServerPort.Size = new Size(105, 27);
            numServerPort.Minimum = 1024;
            numServerPort.Maximum = 65535;
            numServerPort.Value = NetworkConfig.ServerPort >= 1024 && NetworkConfig.ServerPort <= 65535 ? NetworkConfig.ServerPort : 8888;
            numServerPort.Font = new Font("Segoe UI", 9.5F);

            // USERNAME LABEL
            Label lblUsername =
                new Label();
            lblUsername.Text =
                "Tên đăng nhập:";
            lblUsername.Location =
                new Point(
                    45,
                    158);
            lblUsername.Size =
                new Size(
                    330,
                    20);
            lblUsername.Font =
                new Font(
                    "Segoe UI",
                    9F);
            // USERNAME TEXTBOX
            txtUsername =
                new TextBox();
            txtUsername.Location =
                new Point(
                    45,
                    180);
            txtUsername.Size =
                new Size(
                    330,
                    28);
            txtUsername.MaxLength =
                50;
            txtUsername.Font =
                new Font(
                    "Segoe UI",
                    10F);
            // PASSWORD LABEL
            Label lblPassword =
                new Label();
            lblPassword.Text =
                "Mật khẩu:";
            lblPassword.Location =
                new Point(
                    45,
                    218);
            lblPassword.Size =
                new Size(
                    330,
                    20);
            lblPassword.Font =
                new Font(
                    "Segoe UI",
                    9F);
            // PASSWORD TEXTBOX
            txtPassword =
                new TextBox();
            txtPassword.Location =
                new Point(
                    45,
                    240);
            txtPassword.Size =
                new Size(
                    330,
                    28);
            txtPassword.MaxLength =
                100;
            // Ẩn mật khẩu mặc định
            txtPassword.UseSystemPasswordChar =
                true;
            txtPassword.Font =
                new Font(
                    "Segoe UI",
                    10F);
            // SHOW PASSWORD
            chkShowPassword =
                new CheckBox();
            chkShowPassword.Text =
                "Hiện mật khẩu";
            chkShowPassword.Location =
                new Point(
                    45,
                    276);
            chkShowPassword.AutoSize =
                true;
            chkShowPassword.Font =
                new Font(
                    "Segoe UI",
                    8.5F);
            chkShowPassword.Margin =
                new Padding(0);
            chkShowPassword.Padding =
                new Padding(0);
            chkShowPassword.Cursor =
                Cursors.Hand;
            chkShowPassword.CheckedChanged +=
                ChkShowPassword_CheckedChanged;
            // STATUS
            lblStatus =
                new Label();
            lblStatus.Text = "";
            lblStatus.Location =
                new Point(
                    45,
                    305);
            lblStatus.Size =
                new Size(
                    330,
                    45);
            lblStatus.TextAlign =
                ContentAlignment.MiddleLeft;
            lblStatus.ForeColor =
                Color.IndianRed;
            lblStatus.Font =
                new Font(
                    "Segoe UI",
                    8.5F);
            // LOGIN BUTTON
            btnLogin =
                new Button();
            btnLogin.Text =
                "Đăng nhập";
            btnLogin.Location =
                new Point(
                    45,
                    360);
            btnLogin.Size =
                new Size(
                    330,
                    42);
            btnLogin.BackColor =
                Color.FromArgb(
                    30,
                    90,
                    180);
            btnLogin.ForeColor =
                Color.White;
            btnLogin.FlatStyle =
                FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize =
                0;
            btnLogin.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold);
            btnLogin.Cursor =
                Cursors.Hand;
            btnLogin.UseVisualStyleBackColor =
                false;
            btnLogin.Click +=
                BtnLogin_Click;
            // REGISTER LINK
            lnkRegister =
                new LinkLabel();
            lnkRegister.Text =
                "Chưa có tài khoản? Đăng ký ngay";
            lnkRegister.Location =
                new Point(
                    0,
                    420);
            lnkRegister.Size =
                new Size(
                    420,
                    30);
            lnkRegister.TextAlign =
                ContentAlignment.MiddleCenter;
            lnkRegister.Font =
                new Font(
                    "Segoe UI",
                    9F);
            lnkRegister.LinkColor =
                Color.FromArgb(
                    30,
                    90,
                    180);
            lnkRegister.Cursor =
                Cursors.Hand;
            lnkRegister.LinkClicked +=
                LnkRegister_LinkClicked;
            // ADD CONTROLS
            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblServerIp);
            Controls.Add(txtServerIp);
            Controls.Add(lblServerPort);
            Controls.Add(numServerPort);
            Controls.Add(lblUsername);
            Controls.Add(txtUsername);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(chkShowPassword);
            Controls.Add(lblStatus);
            Controls.Add(btnLogin);
            Controls.Add(lnkRegister);
            // ENTER ĐỂ ĐĂNG NHẬP
            AcceptButton =
                btnLogin;
            txtPassword.KeyDown +=
                TxtPassword_KeyDown;
        }
        // HIỆN / ẨN MẬT KHẨU
        private void ChkShowPassword_CheckedChanged(
            object? sender,
            EventArgs e)
        {
            if (chkShowPassword.Checked)
            {
                // Hiện mật khẩu
                txtPassword.UseSystemPasswordChar =
                    false;
            }
            else
            {
                // Ẩn mật khẩu
                txtPassword.UseSystemPasswordChar =
                    true;
            }
        }
        // ENTER ĐỂ ĐĂNG NHẬP
        private void TxtPassword_KeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnLogin_Click(
                    btnLogin,
                    EventArgs.Empty);
            }
        }
        // LOGIN
        private async void BtnLogin_Click(
            object? sender,
            EventArgs e)
        {
            string username =
                txtUsername.Text.Trim();
            string password =
                txtPassword.Text;
            // USERNAME
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowStatus(
                    "Vui lòng nhập tên đăng nhập.");
                txtUsername.Focus();
                return;
            }
            // PASSWORD
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatus(
                    "Vui lòng nhập mật khẩu.");
                txtPassword.Focus();
                return;
            }
            btnLogin.Enabled =
                false;
            try
            {
                // KẾT NỐI SERVER
                bool connected =
                    await EnsureConnectedAsync();
                if (!connected)
                {
                    btnLogin.Enabled =
                        true;
                    return;
                }
                // LOGIN
                ShowStatus(
                    "Đang đăng nhập...",
                    false);
                authService.RequestLogin(
                    username,
                    password);
            }
            catch (Exception ex)
            {
                ShowStatus(
                    ex.Message);
                btnLogin.Enabled =
                    true;
            }
        }
        // REGISTER
        private async void LnkRegister_LinkClicked(
            object? sender,
            LinkLabelLinkClickedEventArgs e)
        {
            lnkRegister.Enabled =
                false;
            try
            {
                bool connected =
                    await EnsureConnectedAsync();
                if (!connected)
                {
                    return;
                }
                ShowStatus("");
                using (
                    var registerForm =
                        new RegisterForm(
                            authService))
                {
                    registerForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                ShowStatus(
                    ex.Message);
            }
            finally
            {
                lnkRegister.Enabled =
                    true;
            }
        }
        // ENSURE CONNECTION
        private async Task<bool> EnsureConnectedAsync()
        {
            string ip = txtServerIp.Text.Trim();
            int port = (int)numServerPort.Value;

            if (string.IsNullOrWhiteSpace(ip))
            {
                ShowStatus("Vui lòng nhập địa chỉ IP/Host của Server.");
                txtServerIp.Focus();
                return false;
            }

            // Nếu đã kết nối với cùng cấu hình
            if (authService.IsConnected && NetworkConfig.ServerIp == ip && NetworkConfig.ServerPort == port)
            {
                return true;
            }

            // Nếu đổi IP/Port khác trong khi đang kết nối, ngắt kết nối cũ
            if (authService.IsConnected && (NetworkConfig.ServerIp != ip || NetworkConfig.ServerPort != port))
            {
                authService.Connection.Disconnect();
            }

            // Lưu cấu hình vào file để lần sau dùng lại
            NetworkConfig.ServerIp = ip;
            NetworkConfig.ServerPort = port;
            NetworkConfig.Save();

            ShowStatus(
                $"Đang kết nối tới Server {ip}:{port}...",
                false);

            bool connected = await Task.Run(
                () => authService.Connect(ip, port));

            if (!connected)
            {
                ShowStatus(
                    $"Không thể kết nối tới {ip}:{port}.\nKiểm tra lại IP, Port và tường lửa (Firewall).",
                    true);
            }

            return connected;
        }
        // LOGIN SUCCESS
        private void OnLoginSucceeded(
            User user)
        {
            RunOnUiThread(() =>
            {
                LoggedInUser =
                    user;
                var clientForm =
                    new ClientForm(
                        user,
                        authService.Connection);
                clientForm.FormClosed +=
                    (s, args) =>
                    {
                        if (clientForm.IsLoggingOut)
                        {
                            txtPassword.Clear();
                            lblStatus.Text = "";
                            btnLogin.Enabled = true;
                            Show();
                            BringToFront();
                            txtPassword.Focus();
                        }
                        else
                        {
                            Close();
                        }
                    };
                clientForm.Show();
                Hide();
            });
        }
        // LOGIN FAILED
        private void OnLoginFailed(
            string errorMessage)
        {
            RunOnUiThread(() =>
            {
                btnLogin.Enabled =
                    true;
                ShowStatus(
                    errorMessage);
            });
        }
        // FORM CLOSED
        private void LoginForm_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            authService.LoginSucceeded -=
                OnLoginSucceeded;
            authService.LoginFailed -=
                OnLoginFailed;
        }
        // STATUS
        private void ShowStatus(
            string message,
            bool isError = true)
        {
            lblStatus.Text =
                message;
            lblStatus.ForeColor =
                isError
                    ? Color.IndianRed
                    : Color.SteelBlue;
        }
        // UI THREAD
        private void RunOnUiThread(
            Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
