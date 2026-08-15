using System;
using System.Drawing;
using System.Windows.Forms;
using ChatTCP.Client.Forms;

namespace ChatTCP.Client.UserControls
{
    public class EmojiInputBar : UserControl
    {
        public event EventHandler<string>? SendClicked;

        private TextBox txtInput = new TextBox();
        private Button btnEmoji = new Button();
        private Button btnSend = new Button();

        public EmojiInputBar()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Size = new Size(400, 40);

            btnEmoji.Text = "😊";
            btnEmoji.Size = new Size(36, 32);
            btnEmoji.Location = new Point(4, 4);
            btnEmoji.Font = new Font("Segoe UI Emoji", 11F);
            btnEmoji.Click += BtnEmoji_Click;

            txtInput.Location = new Point(44, 8);
            txtInput.Size = new Size(270, 24);
            txtInput.Font = new Font("Segoe UI", 9.5F);

            btnSend.Text = "Gửi";
            btnSend.Size = new Size(70, 32);
            btnSend.Location = new Point(320, 4);
            btnSend.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    SendClicked?.Invoke(this, txtInput.Text);
                    txtInput.Clear();
                }
            };

            this.Controls.Add(btnEmoji);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnSend);
        }

        private void BtnEmoji_Click(object? sender, EventArgs e)
        {
            using (var picker = new EmojiPickerForm())
            {
                Point location = btnEmoji.PointToScreen(new Point(0, -picker.Height));
                picker.Location = location;
                if (picker.ShowDialog() == DialogResult.OK && picker.SelectedEmoji != null)
                {
                    txtInput.AppendText(picker.SelectedEmoji);
                }
            }
        }
    }
}
