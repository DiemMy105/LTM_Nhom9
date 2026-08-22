using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatTCP.Client.UserControls
{
    public class UserItem : UserControl
    {
        public int UserId { get; private set; }

        private PictureBox picAvatar;
        private Label lblName;
        private Panel pnlStatus;

        public UserItem()
        {
            this.Size = new Size(250, 60);
            this.BackColor = Color.White;
            this.Cursor = Cursors.Hand;

            picAvatar = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.LightGray
            };

            lblName = new Label
            {
                Location = new Point(60, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            pnlStatus = new Panel
            {
                Size = new Size(12, 12),
                Location = new Point(40, 40),
                BackColor = Color.Gray
            };

            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, 12, 12);
            pnlStatus.Region = new Region(path);

            this.Controls.Add(pnlStatus);
            this.Controls.Add(picAvatar);
            this.Controls.Add(lblName);
        }

        public void UpdateInfo(int userId, string userName, bool isOnline)
        {
            UserId = userId;
            lblName.Text = userName;
            pnlStatus.BackColor = isOnline ? Color.LimeGreen : Color.Gray;
        }
    }
}