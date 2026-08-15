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

        private Label lblSender = new Label();
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
            this.Padding = new Padding(8);

            lblSender.AutoSize = true;
            lblSender.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSender.ForeColor = Color.DarkBlue;

            lblReplyQuote.AutoSize = true;
            lblReplyQuote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblReplyQuote.ForeColor = Color.DimGray;
            lblReplyQuote.Visible = false;

            lblContent.AutoSize = true;
            lblContent.Font = new Font("Segoe UI", 9.5F);

            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI", 7.5F);
            lblTime.ForeColor = Color.Gray;

            ToolStripMenuItem miReply = new ToolStripMenuItem("Trả lời");
            miReply.Click += (s, e) => { if (_message != null) ReplyClicked?.Invoke(this, _message); };
            contextMenu.Items.Add(miReply);

            this.ContextMenuStrip = contextMenu;
            lblContent.ContextMenuStrip = contextMenu;
        }

        public void SetMessage(ChatMessage message, bool isSelf)
        {
            _message = message;
            lblSender.Text = message.SenderName;
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
        }
    }
}
