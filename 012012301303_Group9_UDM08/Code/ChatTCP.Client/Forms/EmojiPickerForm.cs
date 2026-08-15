using System;
using System.Drawing;
using System.Windows.Forms;
using ChatTCP.Client.Services;

namespace ChatTCP.Client.Forms
{
    public class EmojiPickerForm : Form
    {
        public string? SelectedEmoji { get; private set; }

        private FlowLayoutPanel panelEmojis = new FlowLayoutPanel();

        public EmojiPickerForm()
        {
            InitializeUI();
            LoadEmojis();
        }

        private void InitializeUI()
        {
            this.Text = "Chọn Emoji";
            this.Size = new Size(260, 220);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;

            panelEmojis.Dock = DockStyle.Fill;
            panelEmojis.AutoScroll = true;
            panelEmojis.Padding = new Padding(4);

            this.Controls.Add(panelEmojis);
        }

        private void LoadEmojis()
        {
            var emojis = EmojiService.GetPopularEmojis();
            foreach (var emoji in emojis)
            {
                Button btn = new Button
                {
                    Text = emoji,
                    Size = new Size(36, 36),
                    Font = new Font("Segoe UI Emoji", 12F),
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(2)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    SelectedEmoji = emoji;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                panelEmojis.Controls.Add(btn);
            }
        }
    }
}
