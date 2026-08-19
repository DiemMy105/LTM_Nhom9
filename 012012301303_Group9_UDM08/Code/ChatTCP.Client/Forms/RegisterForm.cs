using ChatTCP.Client.Services;
using ChatTCP.Shared.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatTCP.Client.Forms
{
    public class RegisterForm : Form
    {
        private readonly AuthService authService;

        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtDisplayName = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly TextBox txtConfirmPassword = new TextBox();
        private readonly CheckBox chkShowPassword = new CheckBox();

        private readonly Button btnRegister = new Button();
        private readonly Button btnCancel = new Button();
        private readonly Label lblStatus = new Label();

        public User? RegisteredUser { get; private set; }

        public RegisterForm(AuthService authService)
        {
            this.authService = authService;

            InitializeUi();

            authService.RegisterSucceeded += OnRegisterSucceeded;
            authService.RegisterFailed += OnRegisterFailed;

            FormClosed += RegisterForm_FormClosed;
        }

        private void InitializeUi()
        {
            Text = "Đăng ký tài khoản";
            Size = new Size(420, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label
            {
                Text = "Tạo tài khoản mới",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            };

            Label lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(20, 60),
                AutoSize = true
            };

            txtUsername.Location = new Point(20, 83);
            txtUsername.Size = new Size(365, 27);
            txtUsername.MaxLength = 50;

            Label lblDisplayName = new Label
            {
                Text = "Tên hiển thị:",
                Location = new Point(20, 120),
                AutoSize = true
            };

            txtDisplayName.Location = new Point(20, 143);
            txtDisplayName.Size = new Size(365, 27);
            txtDisplayName.MaxLength = 100;

            Label lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(20, 180),
                AutoSize = true
            };

            txtPassword.Location = new Point(20, 203);
            txtPassword.Size = new Size(365, 27);
            txtPassword.PasswordChar = '●';
            txtPassword.MaxLength = 100;

            Label lblConfirmPassword = new Label
            {
                Text = "Xác nhận mật khẩu:",
                Location = new Point(20, 240),
                AutoSize = true
            };

            txtConfirmPassword.Location = new Point(20, 263);
            txtConfirmPassword.Size = new Size(365, 27);
            txtConfirmPassword.PasswordChar = '●';
            txtConfirmPassword.MaxLength = 100;

            chkShowPassword.Text = "Hiện mật khẩu";
            chkShowPassword.Location = new Point(20, 296);
            chkShowPassword.AutoSize = true;
            chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;

            lblStatus.Text = "";
            lblStatus.Location = new Point(20, 325);
            lblStatus.Size = new Size(365, 20);
            lblStatus.ForeColor = Color.IndianRed;
            lblStatus.Font = new Font("Segoe UI", 8.5F);

            btnRegister.Text = "Đăng ký";
            btnRegister.Location = new Point(195, 365);
            btnRegister.Size = new Size(90, 32);
            btnRegister.Click += BtnRegister_Click;

            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(295, 365);
            btnCancel.Size = new Size(90, 32);
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.Add(lblTitle);
            Controls.Add(lblUsername);
            Controls.Add(txtUsername);
            Controls.Add(lblDisplayName);
            Controls.Add(txtDisplayName);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblConfirmPassword);
            Controls.Add(txtConfirmPassword);
            Controls.Add(chkShowPassword);
            Controls.Add(lblStatus);
            Controls.Add(btnRegister);
            Controls.Add(btnCancel);

            AcceptButton = btnRegister;
            CancelButton = btnCancel;
        }

        private void ChkShowPassword_CheckedChanged(object? sender, EventArgs e)
        {
            char pwdChar = chkShowPassword.Checked ? '\0' : '●';
            txtPassword.PasswordChar = pwdChar;
            txtConfirmPassword.PasswordChar = pwdChar;
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string displayName = txtDisplayName.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowStatus("Vui lòng nhập tên đăng nhập.");
                txtUsername.Focus();
                return;
            }

            if (username.Contains('|') || username.Contains(' '))
            {
                ShowStatus("Tên đăng nhập không được chứa khoảng trắng hoặc ký tự |.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Vui lòng nhập mật khẩu.");
                txtPassword.Focus();
                return;
            }

            if (password.Length < 6)
            {
                ShowStatus("Mật khẩu phải có ít nhất 6 ký tự.");
                txtPassword.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                ShowStatus("Mật khẩu xác nhận không khớp.");
                txtConfirmPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = username;
            }

            try
            {
                btnRegister.Enabled = false;
                ShowStatus("Đang đăng ký...", isError: false);

                authService.RequestRegister(username, password, displayName);
            }
            catch (Exception ex)
            {
                btnRegister.Enabled = true;
                ShowStatus(ex.Message);
            }
        }

        private void OnRegisterSucceeded(User user)
        {
            RunOnUiThread(() =>
            {
                RegisteredUser = user;
                DialogResult = DialogResult.OK;
                Close();
            });
        }

        private void OnRegisterFailed(string errorMessage)
        {
            RunOnUiThread(() =>
            {
                btnRegister.Enabled = true;
                ShowStatus(errorMessage);
            });
        }

        private void RegisterForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            authService.RegisterSucceeded -= OnRegisterSucceeded;
            authService.RegisterFailed -= OnRegisterFailed;
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