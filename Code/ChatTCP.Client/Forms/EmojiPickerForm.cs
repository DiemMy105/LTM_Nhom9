using System;
using System.Drawing;
using System.Windows.Forms;
using ChatTCP.Client.Services;


namespace ChatTCP.Client.Forms
{
    public class EmojiPickerForm : Form
    {
        public string? SelectedEmoji { get; private set; }
        public event Action<string>? EmojiSelected;


        private Panel panelHeader = new Panel();
        private Label lblTitle = new Label();
        private Button btnClose = new Button();
        private FlowLayoutPanel panelEmojis = new FlowLayoutPanel();


        public EmojiPickerForm()
        {
            InitializeUI();
            LoadEmojis();
        }


        private void InitializeUI()
        {
            this.Text = "Chọn Emoji";
            this.Size = new Size(330, 220);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(245, 246, 248);


            // Header bar
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 28;
            panelHeader.BackColor = Color.FromArgb(235, 238, 242);
            panelHeader.Padding = new Padding(8, 0, 4, 0);


            lblTitle.Text = "Biểu cảm (Emoji)";
            lblTitle.Dock = DockStyle.Left;
            lblTitle.AutoSize = true;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(70, 75, 85);


            btnClose.Text = "✕";
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 26;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btnClose.ForeColor = Color.FromArgb(100, 100, 100);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();


            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(btnClose);


            // Flow panel for emojis
            panelEmojis.Dock = DockStyle.Fill;
            panelEmojis.AutoScroll = true;
            panelEmojis.Padding = new Padding(6);
            panelEmojis.BackColor = Color.White;


            this.Controls.Add(panelEmojis);
            this.Controls.Add(panelHeader);
        }


        private void LoadEmojis()
        {
            var emojis = EmojiService.GetPopularEmojis();
            foreach (var emoji in emojis)
            {
                Button btn = new Button
                {
                    Text = emoji,
                    Size = new Size(38, 38),
                    Font = new Font("Segoe UI Emoji", 13F),
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 240, 255);


                btn.Click += (s, e) =>
                {
                    SelectedEmoji = emoji;
                    EmojiSelected?.Invoke(emoji);
                };


                panelEmojis.Controls.Add(btn);
            }
        }
    }
}



