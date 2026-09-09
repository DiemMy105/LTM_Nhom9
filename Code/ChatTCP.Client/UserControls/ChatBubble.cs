using System;
using System.Drawing;
using System.Windows.Forms;
using ChatTCP.Client.Utils;
using ChatMessage = ChatTCP.Shared.Models.Message;


namespace ChatTCP.Client.UserControls
{
    public class ChatBubble : UserControl
    {
        public event EventHandler<ChatMessage>? ReplyClicked;


        public event EventHandler<ChatMessage>? ForwardClicked;


        private const int MaxContentWidth = 260;


        private Label lblSender = new Label();
        private Label lblForwarded = new Label();
        private Label lblReplyQuote = new Label();
        private Label lblContent = new Label();
        private Label lblTime = new Label();
        private ContextMenuStrip contextMenu = new ContextMenuStrip();
        private ChatMessage? _message;


        public ChatBubble()
        {
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(8);
            this.BackColor = Color.WhiteSmoke;


            lblSender.AutoSize = true;
            lblSender.Location = new Point(8, 6);
            lblSender.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSender.ForeColor = Color.DarkBlue;


            lblForwarded.AutoSize = true;
            lblForwarded.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblForwarded.ForeColor = Color.Gray;
            lblForwarded.Text = "↪ Đã chuyển tiếp";
            lblForwarded.Visible = false;


            lblReplyQuote.AutoSize = true;
            lblReplyQuote.MaximumSize = new Size(MaxContentWidth, 0);
            lblReplyQuote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblReplyQuote.ForeColor = Color.DimGray;
            lblReplyQuote.Visible = false;


            lblContent.AutoSize = true;
            lblContent.MaximumSize = new Size(MaxContentWidth, 0);
            lblContent.Font = new Font("Segoe UI", 9.5F);


            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI", 7.5F);
            lblTime.ForeColor = Color.Gray;


            ToolStripMenuItem miReply = new ToolStripMenuItem("Trả lời");
            miReply.Click += (s, e) => { if (_message != null) ReplyClicked?.Invoke(this, _message); };


            ToolStripMenuItem miForward = new ToolStripMenuItem("Chuyển tiếp");
            miForward.Click += (s, e) => { if (_message != null) ForwardClicked?.Invoke(this, _message); };


            contextMenu.Items.Add(miReply);
            contextMenu.Items.Add(miForward);


            this.ContextMenuStrip = contextMenu;
            lblContent.ContextMenuStrip = contextMenu;


            this.Controls.Add(lblSender);
            this.Controls.Add(lblForwarded);
            this.Controls.Add(lblReplyQuote);
            this.Controls.Add(lblContent);
            this.Controls.Add(lblTime);


            lblSender.SizeChanged += (s, e) => LayoutBubble();
            lblForwarded.SizeChanged += (s, e) => LayoutBubble();
            lblReplyQuote.SizeChanged += (s, e) => LayoutBubble();
            lblContent.SizeChanged += (s, e) => LayoutBubble();
        }


        public void SetMessage(ChatMessage message, bool isSelf)
        {
            _message = message;


            lblSender.Text = message.SenderName;
            lblSender.Visible = !isSelf && !string.IsNullOrWhiteSpace(message.SenderName);


            lblForwarded.Visible = message.IsForward;


            lblContent.Text = message.Content;
            lblTime.Text = message.Timestamp.ToString("HH:mm");


            if (!string.IsNullOrEmpty(message.ReplyToSenderName))
            {
                lblReplyQuote.Text = MessageHelper.FormatReplyPreview(message);
                lblReplyQuote.Visible = true;
            }
            else
            {
                lblReplyQuote.Visible = false;
            }


            this.BackColor = isSelf ? Color.LightBlue : Color.WhiteSmoke;


            LayoutBubble();
        }


        private void LayoutBubble()
        {
            int y = 6;
            int x = 8;


            if (lblSender.Visible)
            {
                lblSender.Location = new Point(x, y);
                y += lblSender.Height + 2;
            }


            if (lblForwarded.Visible)
            {
                lblForwarded.Location = new Point(x, y);
                y += lblForwarded.Height + 2;
            }


            if (lblReplyQuote.Visible)
            {
                lblReplyQuote.Location = new Point(x, y);
                y += lblReplyQuote.Height + 2;
            }


            lblContent.Location = new Point(x, y);
            y += lblContent.Height + 2;


            lblTime.Location = new Point(x, y);
            y += lblTime.Height + 6;


            int contentWidth = Math.Max(lblContent.Width, lblReplyQuote.Visible ? lblReplyQuote.Width : 0);
            contentWidth = Math.Max(contentWidth, lblSender.Visible ? lblSender.Width : 0);
            contentWidth = Math.Max(contentWidth, lblForwarded.Visible ? lblForwarded.Width : 0);


            this.Size = new Size(contentWidth + x + 8, y);
        }
    }
}



