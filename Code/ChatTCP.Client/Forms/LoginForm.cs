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

            ClientSize = new Size(400, 480);

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

            // Đã sửa: ChatTCP -> Chat TCP
            lblTitle.Text = "Chat TCP";

            lblTitle.Location =
                new Point(
                    0,
                    30);

            lblTitle.Size =
                new Size(
                    400,
                    50);

            lblTitle.TextAlign =
                ContentAlignment.MiddleCenter;

            lblTitle.ForeColor =
                Color.FromArgb(
                    30,
                    90,
                    180);

            lblTitle.Font =
                new Font(
                    "Segoe UI",
                    22F,
                    FontStyle.Bold);

            // SUBTITLE

            Label lblSubtitle =
                new Label();

            lblSubtitle.Text =
                "Đăng nhập để bắt đầu trò chuyện";

            lblSubtitle.Location =
                new Point(
                    0,
                    82);

            lblSubtitle.Size =
                new Size(
                    400,
                    28);

            lblSubtitle.TextAlign =
                ContentAlignment.MiddleCenter;

            lblSubtitle.ForeColor =
                Color.Gray;

            lblSubtitle.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            // USERNAME LABEL

            Label lblUsername =
                new Label();

            lblUsername.Text =
                "Tên đăng nhập:";

            lblUsername.Location =
                new Point(
                    45,
                    125);

            lblUsername.Size =
                new Size(
                    310,
                    24);

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
                    150);

            txtUsername.Size =
                new Size(
                    310,
                    30);

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
                    195);

            lblPassword.Size =
                new Size(
                    310,
                    24);

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
                    220);

            txtPassword.Size =
                new Size(
                    310,
                    30);

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

            // Đã sửa thành đầy đủ
            chkShowPassword.Text =
                "Hiện mật khẩu";

            chkShowPassword.Location =
                new Point(
                    45,
                    260);

            // Cho tự động giãn theo chữ
            chkShowPassword.AutoSize =
                true;

            chkShowPassword.Font =
                new Font(
                    "Segoe UI",
                    9F);

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
                    290);

            lblStatus.Size =
                new Size(
                    310,
                    30);

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
                    330);

            btnLogin.Size =
                new Size(
                    310,
                    40);

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
                    390);

            lnkRegister.Size =
                new Size(
                    400,
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
                    ShowStatus(
                        "Không thể kết nối tới Server. " +
                        "Kiểm tra lại IP/Port.");

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

            ShowStatus(
                "Đang kết nối tới Server...",
                false);

            try
            {
                bool connected =
                    await EnsureConnectedAsync();

                if (!connected)
                {
                    ShowStatus(
                        "Không thể kết nối tới Server. " +
                        "Kiểm tra lại IP/Port.");

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
            if (authService.IsConnected)
            {
                return true;
            }

            ShowStatus(
                "Đang kết nối tới Server...",
                false);

            return await Task.Run(
                () =>
                    authService.Connect(
                        NetworkConfig.ServerIp,
                        NetworkConfig.ServerPort));
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