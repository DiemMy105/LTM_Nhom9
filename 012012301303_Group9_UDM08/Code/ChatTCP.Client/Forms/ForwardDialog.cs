using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChatTCP.Shared.Models;
using ChatMessage = ChatTCP.Shared.Models.Message;

namespace ChatTCP.Client.Forms
{
    public class ForwardDialog : Form
    {
        public int? SelectedReceiverId { get; private set; }
        public int? SelectedGroupId { get; private set; }
        public string TargetName { get; private set; } = string.Empty;

        private ListBox lbTargets = new ListBox();
        private Button btnForward = new Button();
        private Button btnCancel = new Button();
        private Label lblHeader = new Label();
        private List<(string Name, int? UserId, int? GroupId)> _items = new List<(string, int?, int?)>();

        public ForwardDialog(List<User> users, List<Group> groups, ChatMessage messageToForward)
        {
            InitializeUI(messageToForward);
            LoadTargets(users, groups);
        }

        private void InitializeUI(ChatMessage messageToForward)
        {
            this.Text = "Chuyển tiếp tin nhắn";
            this.Size = new Size(350, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblHeader.Text = $"Chuyển tiếp: \"{(messageToForward.Content.Length > 25 ? messageToForward.Content.Substring(0, 22) + "..." : messageToForward.Content)}\"";
            lblHeader.Location = new Point(12, 12);
            lblHeader.Size = new Size(310, 30);
            lblHeader.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

            lbTargets.Location = new Point(12, 48);
            lbTargets.Size = new Size(310, 270);
            lbTargets.Font = new Font("Segoe UI", 9.5F);

            btnForward.Text = "Chuyển tiếp";
            btnForward.Location = new Point(136, 330);
            btnForward.Size = new Size(90, 32);
            btnForward.DialogResult = DialogResult.OK;
            btnForward.Click += BtnForward_Click;

            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(232, 330);
            btnCancel.Size = new Size(90, 32);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblHeader);
            this.Controls.Add(lbTargets);
            this.Controls.Add(btnForward);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnForward;
            this.CancelButton = btnCancel;
        }

        private void LoadTargets(List<User> users, List<Group> groups)
        {
            lbTargets.Items.Clear();
            _items.Clear();

            foreach (var group in groups)
            {
                string display = $"👥 Nhóm: {group.GroupName}";
                lbTargets.Items.Add(display);
                _items.Add((group.GroupName, null, group.GroupId));
            }

            foreach (var user in users)
            {
                string display = $"👤 {user.DisplayName} (@{user.Username})";
                lbTargets.Items.Add(display);
                _items.Add((user.DisplayName, user.UserId, null));
            }

            if (lbTargets.Items.Count > 0)
            {
                lbTargets.SelectedIndex = 0;
            }
        }

        private void BtnForward_Click(object? sender, EventArgs e)
        {
            int index = lbTargets.SelectedIndex;
            if (index >= 0 && index < _items.Count)
            {
                var item = _items[index];
                TargetName = item.Name;
                SelectedReceiverId = item.UserId;
                SelectedGroupId = item.GroupId;
            }
        }
    }
}
