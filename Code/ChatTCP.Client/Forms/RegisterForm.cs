using ChatTCP.Client.Services;
using ChatTCP.Client.Utils;
using ChatTCP.Shared.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
namespace ChatTCP.Client.Forms
{
    public class RegisterForm : Form
    {
        private readonly AuthService authService;
        // AVATAR
        private readonly PictureBox picAvatarPreview =
            new PictureBox();
        private readonly Button btnChooseAvatar =
            new Button();
        // Đường dẫn ảnh avatar người dùng chọn
        private string? _selectedAvatarPath;
        // Cho phép form khác lấy đường dẫn avatar
        public string? SelectedAvatarPath =>
            _selectedAvatarPath;
        // INPUT
        private readonly TextBox txtUsername =
            new TextBox();
        private readonly TextBox txtDisplayName =
            new TextBox();
        private readonly TextBox txtPassword =
            new TextBox();
        private readonly TextBox txtConfirmPassword =
            new TextBox();
        private readonly CheckBox chkShowPassword =
            new CheckBox();
        // BUTTONS
        private readonly Button btnRegister =
            new Button();
        private readonly Button btnCancel =
            new Button();
        // STATUS
        private readonly Label lblStatus =
            new Label();
        // RESULT
        public User? RegisteredUser
        {
            get;
            private set;
        }
        // CONSTRUCTOR
        public RegisterForm(AuthService authService)
        {
            this.authService = authService;
            InitializeUi();
            authService.RegisterSucceeded +=
                OnRegisterSucceeded;
            authService.RegisterFailed +=
                OnRegisterFailed;
            FormClosed +=
                RegisterForm_FormClosed;
        }
        // INITIALIZE UI
        private void InitializeUi()
        {
            Text = "Đăng ký tài khoản";
            Size = new Size(420, 600);
            StartPosition =
                FormStartPosition.CenterParent;
            FormBorderStyle =
                FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font =
                new Font(
                    "Segoe UI",
                    9F);
            // AVATAR
            picAvatarPreview.Size =
                new Size(90, 90);
            picAvatarPreview.Location =
                new Point(
                    (ClientSize.Width - 90) / 2,
                    15);
            picAvatarPreview.SizeMode =
                PictureBoxSizeMode.Zoom;
            picAvatarPreview.BackColor =
                Color.WhiteSmoke;
            picAvatarPreview.Anchor =
                AnchorStyles.Top;
            // Avatar mặc định
            try
            {
                picAvatarPreview.Image =
                    ImageUtils.LoadAvatarResized(
                        ImageUtils.DefaultAvatarFileName,
                        90,
                        90);
            }
            catch
            {
                // Nếu không tìm thấy avatar mặc định
                // thì vẫn giữ PictureBox trống.
            }
            ImageUtils.MakeCircle(
                picAvatarPreview);
            // BUTTON CHỌN AVATAR
            btnChooseAvatar.Text =
                "📷 Chọn ảnh";
            btnChooseAvatar.Size =
                new Size(105, 30);
            btnChooseAvatar.Location =
                new Point(
                    (ClientSize.Width - 105) / 2,
                    110);
            btnChooseAvatar.FlatStyle =
                FlatStyle.Flat;
            btnChooseAvatar.Cursor =
                Cursors.Hand;
            btnChooseAvatar.Click +=
                BtnChooseAvatar_Click;
            // TITLE
            Label lblTitle =
                new Label
                {
                    Text = "Tạo tài khoản mới",
                    Location =
                        new Point(
                            20,
                            150),
                    AutoSize = true,
                    Font =
                        new Font(
                            "Segoe UI",
                            13F,
                            FontStyle.Bold)
                };
            // USERNAME
            Label lblUsername =
                new Label
                {
                    Text = "Tên đăng nhập:",
                    Location =
                        new Point(
                            20,
                            195),
                    AutoSize = true
                };
            txtUsername.Location =
                new Point(
                    20,
                    218);
            txtUsername.Size =
                new Size(
                    365,
                    27);
            txtUsername.MaxLength = 50;
            // DISPLAY NAME
            Label lblDisplayName =
                new Label
                {
                    Text = "Tên hiển thị:",
                    Location =
                        new Point(
                            20,
                            255),
                    AutoSize = true
                };
            txtDisplayName.Location =
                new Point(
                    20,
                    278);
            txtDisplayName.Size =
                new Size(
                    365,
                    27);
            txtDisplayName.MaxLength = 100;
            // PASSWORD
            Label lblPassword =
                new Label
                {
                    Text = "Mật khẩu:",
                    Location =
                        new Point(
                            20,
                            315),
                    AutoSize = true
                };
            txtPassword.Location =
                new Point(
                    20,
                    338);
            txtPassword.Size =
                new Size(
                    365,
                    27);
            txtPassword.PasswordChar = '●';
            txtPassword.MaxLength = 100;
            // CONFIRM PASSWORD
            Label lblConfirmPassword =
                new Label
                {
                    Text = "Xác nhận mật khẩu:",
                    Location =
                        new Point(
                            20,
                            375),
                    AutoSize = true
                };
            txtConfirmPassword.Location =
                new Point(
                    20,
                    398);
            txtConfirmPassword.Size =
                new Size(
                    365,
                    27);
            txtConfirmPassword.PasswordChar =
                '●';
            txtConfirmPassword.MaxLength =
                100;
            // SHOW PASSWORD
            chkShowPassword.Text =
                "Hiện mật khẩu";
            chkShowPassword.Location =
                new Point(
                    20,
                    432);
            chkShowPassword.AutoSize =
                true;
            chkShowPassword.CheckedChanged +=
                ChkShowPassword_CheckedChanged;
            // STATUS
            lblStatus.Text = "";
            lblStatus.Location =
                new Point(
                    20,
                    460);
            lblStatus.Size =
                new Size(
                    365,
                    35);
            lblStatus.ForeColor =
                Color.IndianRed;
            lblStatus.Font =
                new Font(
                    "Segoe UI",
                    8.5F);
            // REGISTER BUTTON
            btnRegister.Text =
                "Đăng ký";
            btnRegister.Location =
                new Point(
                    195,
                    510);
            btnRegister.Size =
                new Size(
                    90,
                    32);
            btnRegister.Click +=
                BtnRegister_Click;
            // CANCEL BUTTON
            btnCancel.Text =
                "Hủy";
            btnCancel.Location =
                new Point(
                    295,
                    510);
            btnCancel.Size =
                new Size(
                    90,
                    32);
            btnCancel.DialogResult =
                DialogResult.Cancel;
            // ADD CONTROLS
            Controls.Add(
                picAvatarPreview);
            Controls.Add(
                btnChooseAvatar);
            Controls.Add(
                lblTitle);
            Controls.Add(
                lblUsername);
            Controls.Add(
                txtUsername);
            Controls.Add(
                lblDisplayName);
            Controls.Add(
                txtDisplayName);
            Controls.Add(
                lblPassword);
            Controls.Add(
                txtPassword);
            Controls.Add(
                lblConfirmPassword);
            Controls.Add(
                txtConfirmPassword);
            Controls.Add(
                chkShowPassword);
            Controls.Add(
                lblStatus);
            Controls.Add(
                btnRegister);
            Controls.Add(
                btnCancel);
            AcceptButton =
                btnRegister;
            CancelButton =
                btnCancel;
        }
        // CHỌN AVATAR
        private void BtnChooseAvatar_Click(
            object? sender,
            EventArgs e)
        {
            using OpenFileDialog dialog =
                new OpenFileDialog();
            dialog.Title =
                "Chọn ảnh đại diện";
            dialog.Filter =
                "Ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Multiselect = false;
            if (dialog.ShowDialog(this)
                != DialogResult.OK)
            {
                return;
            }
            try
            {
                // Lưu đường dẫn ảnh
                _selectedAvatarPath =
                    dialog.FileName;
                // Đọc ảnh vào MemoryStream để không khóa file gốc.
                using FileStream fileStream =
                    new FileStream(
                        _selectedAvatarPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);
                using MemoryStream memoryStream =
                    new MemoryStream();
                fileStream.CopyTo(
                    memoryStream);
                memoryStream.Position = 0;
                using Image tempImage =
                    Image.FromStream(
                        memoryStream);
                // Xóa ảnh cũ
                if (picAvatarPreview.Image != null)
                {
                    Image oldImage =
                        picAvatarPreview.Image;
                    picAvatarPreview.Image = null;
                    oldImage.Dispose();
                }
                // Tạo bản sao ảnh
                picAvatarPreview.Image =
                    new Bitmap(
                        tempImage);
                // Bo tròn avatar
                ImageUtils.MakeCircle(
                    picAvatarPreview);
                ShowStatus(
                    "Đã chọn ảnh đại diện.",
                    isError: false);
            }
            catch (Exception ex)
            {
                _selectedAvatarPath = null;
                MessageBox.Show(
                    "Không thể tải ảnh đại diện.\n\n"
                    + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        // LƯU AVATAR ĐÃ CHỌN VÀO Resources/Avatars/ CỦA CLIENT
        private string? SaveAvatarLocallyOrNull(string username)
        {
            if (string.IsNullOrWhiteSpace(_selectedAvatarPath))
            {
                return null;
            }
            try
            {
                string avatarsDir = Path.Combine(
                    AppContext.BaseDirectory, "Resources", "Avatars");
                if (!Directory.Exists(avatarsDir))
                {
                    Directory.CreateDirectory(avatarsDir);
                }
                string fileName = username + ".png";
                string destPath = Path.Combine(avatarsDir, fileName);
                using FileStream fileStream = new FileStream(
                    _selectedAvatarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using MemoryStream memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                using Image original = Image.FromStream(memoryStream);
                using Image resized = ImageUtils.ResizeImage(original, 200, 200);
                resized.Save(destPath, ImageFormat.Png);
                return fileName;
            }
            catch (Exception ex)
            {
                ShowStatus(
                    "Không thể lưu ảnh đại diện, sẽ dùng avatar mặc định: " + ex.Message,
                    isError: false);
                return null;
            }
        }
        // SHOW / HIDE PASSWORD
        private void ChkShowPassword_CheckedChanged(
            object? sender,
            EventArgs e)
        {
            char pwdChar =
                chkShowPassword.Checked
                    ? '\0'
                    : '●';
            txtPassword.PasswordChar =
                pwdChar;
            txtConfirmPassword.PasswordChar =
                pwdChar;
        }
        // REGISTER
        private void BtnRegister_Click(
            object? sender,
            EventArgs e)
        {
            string username =
                txtUsername.Text.Trim();
            string displayName =
                txtDisplayName.Text.Trim();
            string password =
                txtPassword.Text;
            string confirmPassword =
                txtConfirmPassword.Text;
            // USERNAME
            if (string.IsNullOrWhiteSpace(
                username))
            {
                ShowStatus(
                    "Vui lòng nhập tên đăng nhập.");
                txtUsername.Focus();
                return;
            }
            if (username.Contains('|')
                || username.Contains(' '))
            {
                ShowStatus(
                    "Tên đăng nhập không được chứa "
                    + "khoảng trắng hoặc ký tự |.");
                txtUsername.Focus();
                return;
            }
            // PASSWORD
            if (string.IsNullOrWhiteSpace(
                password))
            {
                ShowStatus(
                    "Vui lòng nhập mật khẩu.");
                txtPassword.Focus();
                return;
            }
            if (password.Length < 6)
            {
                ShowStatus(
                    "Mật khẩu phải có ít nhất 6 ký tự.");
                txtPassword.Focus();
                return;
            }
            // CONFIRM PASSWORD
            if (password != confirmPassword)
            {
                ShowStatus(
                    "Mật khẩu xác nhận không khớp.");
                txtConfirmPassword.Focus();
                return;
            }
            // DISPLAY NAME
            if (string.IsNullOrWhiteSpace(
                displayName))
            {
                displayName = username;
            }
            // REGISTER
            try
            {
                btnRegister.Enabled = false;
                ShowStatus(
                    "Đang đăng ký...",
                    isError: false);
                string? avatarFileName =
                    SaveAvatarLocallyOrNull(username);
                string? avatarData = null;
                if (!string.IsNullOrWhiteSpace(_selectedAvatarPath))
                {
                    avatarData = ImageUtils.ConvertImageFileToBase64(_selectedAvatarPath, 128, 128);
                }
                authService.RequestRegister(
                    username,
                    password,
                    displayName,
                    avatarFileName,
                    avatarData);
            }
            catch (Exception ex)
            {
                btnRegister.Enabled = true;
                ShowStatus(
                    ex.Message);
            }
        }
        // REGISTER SUCCESS
        private void OnRegisterSucceeded(
            User user)
        {
            RunOnUiThread(() =>
            {
                RegisteredUser = user;
                DialogResult =
                    DialogResult.OK;
                Close();
            });
        }
        // REGISTER FAILED
        private void OnRegisterFailed(
            string errorMessage)
        {
            RunOnUiThread(() =>
            {
                btnRegister.Enabled = true;
                ShowStatus(
                    errorMessage);
            });
        }
        // FORM CLOSED
        private void RegisterForm_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            authService.RegisterSucceeded -=
                OnRegisterSucceeded;
            authService.RegisterFailed -=
                OnRegisterFailed;
            // Giải phóng ảnh preview
            if (picAvatarPreview.Image != null)
            {
                Image image =
                    picAvatarPreview.Image;
                picAvatarPreview.Image = null;
                image.Dispose();
            }
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