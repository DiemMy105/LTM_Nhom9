using ChatTCP.Client.Services;
using ChatTCP.Shared.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatTCP.Client.Forms
{
    public class LoginForm : Form
    {
        private readonly AuthService authService;

        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly CheckBox chkShowPassword = new CheckBox();

        private readonly Button btnLogin = new Button();
        private readonly LinkLabel lnkRegister = new LinkLabel();
        private readonly Label lblStatus = new Label();

        public User? LoggedInUser { get; private set; }

        public LoginForm(AuthService authService)
        {
            this.authService = authService;

            InitializeUi();

            authService.LoginSucceeded += OnLoginSucceeded;
            authService.LoginFailed += OnLoginFailed;

            FormClosed += LoginForm_FormClosed;
        }

        private void InitializeUi()
        {
            Text = "ChatTCP - Đăng nhập";
            Size = new Size(400, 430);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label
            {
                Text = "ChatTCP",
                Location = new Point(40, 30),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 90, 180),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold)
            };

            Label lblSubtitle = new Label
            {
                Text = "Đăng nhập để bắt đầu trò chuyện",
                Location = new Point(42, 75),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9.5F)
            };

            Label lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(40, 120),
                AutoSize = true
            };

            txtUsername.Location = new Point(40, 143);
            txtUsername.Size = new Size(320, 27);
            txtUsername.MaxLength = 50;

            Label lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(40, 185),
                AutoSize = true
            };

            txtPassword.Location = new Point(40, 208);
            txtPassword.Size = new Size(320, 27);
            txtPassword.PasswordChar = '●';
            txtPassword.MaxLength = 100;

            chkShowPassword.Text = "Hiện mật khẩu";
            chkShowPassword.Location = new Point(40, 241);
            chkShowPassword.AutoSize = true;
            chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;

            lblStatus.Text = "";
            lblStatus.Location = new Point(40, 270);
            lblStatus.Size = new Size(320, 20);
            lblStatus.ForeColor = Color.IndianRed;
            lblStatus.Font = new Font("Segoe UI", 8.5F);

            btnLogin.Text = "Đăng nhập";
            btnLogin.Location = new Point(40, 300);
            btnLogin.Size = new Size(320, 38);
            btnLogin.BackColor = Color.FromArgb(30, 90, 180);
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLogin.Click += BtnLogin_Click;

            lnkRegister.Text = "Chưa có tài khoản? Đăng ký ngay";
            lnkRegister.Location = new Point(85, 350);
            lnkRegister.AutoSize = true;
            lnkRegister.LinkClicked += LnkRegister_LinkClicked;

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblUsername);
            Controls.Add(txtUsername);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(chkShowPassword);
            Controls.Add(lblStatus);
            Controls.Add(btnLogin);
            Controls.Add(lnkRegister);

            AcceptButton = btnLogin;
        }

        private void ChkShowPassword_CheckedChanged(object? sender, EventArgs e)
        {
            txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowStatus("Vui lòng nhập tên đăng nhập.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Vui lòng nhập mật khẩu.");
                txtPassword.Focus();
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                ShowStatus("Đang kết nối tới Server...", isError: false);

                authService.RequestLogin(username, password);
            }
            catch (Exception ex)
            {
                btnLogin.Enabled = true;
                ShowStatus(ex.Message);
            }
        }

        private void LnkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            // TODO: [TV1] Nên truyền chung 1 instance AuthService (dùng chung
            // TcpClientManager) cho cả LoginForm và RegisterForm thay vì tạo service mới.
            using (var registerForm = new RegisterForm(authService))
            {
                registerForm.ShowDialog(this);
            }
        }

        private void OnLoginSucceeded(User user)
        {
            RunOnUiThread(() =>
            {
                LoggedInUser = user;

                // TODO: [TV2/TV4] Truyền đủ Service thật (ChatService, EmojiService...)
                // vào ClientForm khi các lớp đó đã sẵn sàng. GroupService demo đã được
                // ClientForm tự khởi tạo bên trong (xem GroupService.cs).
                var clientForm = new ClientForm(user);
                clientForm.FormClosed += (s, args) => Close();
                clientForm.Show();
                Hide();
            });
        }

        private void OnLoginFailed(string errorMessage)
        {
            RunOnUiThread(() =>
            {
                btnLogin.Enabled = true;
                ShowStatus(errorMessage);
            });
        }

        private void LoginForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            authService.LoginSucceeded -= OnLoginSucceeded;
            authService.LoginFailed -= OnLoginFailed;
        }

        private void ShowStatus(string message, bool isError = true)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError ? Color.IndianRed : Color.SteelBlue;
        }

        private void RunOnUiThread(Action action)
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