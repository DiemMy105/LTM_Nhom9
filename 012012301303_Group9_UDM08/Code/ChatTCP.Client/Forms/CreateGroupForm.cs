using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChatTCP.Client.Services;
using ChatTCP.Shared.Models;

namespace ChatTCP.Client.Forms
{
    public class CreateGroupForm : Form
    {
        private readonly GroupService groupService;
        private readonly List<User> selectableUsers;

        private readonly TextBox txtGroupName = new TextBox();
        private readonly CheckedListBox clbMembers =
            new CheckedListBox();

        private readonly Button btnCreate = new Button();
        private readonly Button btnCancel = new Button();

        public Group? CreatedGroup { get; private set; }

        public CreateGroupForm(
            GroupService groupService,
            IEnumerable<User> users)
        {
            this.groupService = groupService;

            selectableUsers = users
                .Where(user =>
                    user.UserId != groupService.CurrentUserId)
                .ToList();

            InitializeUi();
            LoadUsers();

            groupService.GroupCreated += OnGroupCreated;
            groupService.CreateGroupFailed += OnCreateGroupFailed;

            FormClosed += CreateGroupForm_FormClosed;
        }

        private void InitializeUi()
        {
            Text = "Tạo nhóm chat";
            Size = new Size(420, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            Label lblGroupName = new Label
            {
                Text = "Tên nhóm:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            txtGroupName.Location = new Point(20, 45);
            txtGroupName.Size = new Size(365, 27);
            txtGroupName.MaxLength = 100;

            Label lblMembers = new Label
            {
                Text = "Chọn thành viên:",
                Location = new Point(20, 90),
                AutoSize = true
            };

            clbMembers.Location = new Point(20, 115);
            clbMembers.Size = new Size(365, 280);
            clbMembers.CheckOnClick = true;

            btnCreate.Text = "Tạo nhóm";
            btnCreate.Location = new Point(195, 410);
            btnCreate.Size = new Size(90, 32);
            btnCreate.Click += BtnCreate_Click;

            btnCancel.Text = "Hủy";
            btnCancel.Location = new Point(295, 410);
            btnCancel.Size = new Size(90, 32);
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.Add(lblGroupName);
            Controls.Add(txtGroupName);
            Controls.Add(lblMembers);
            Controls.Add(clbMembers);
            Controls.Add(btnCreate);
            Controls.Add(btnCancel);

            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        private void LoadUsers()
        {
            foreach (User user in selectableUsers)
            {
                string displayName =
                    string.IsNullOrWhiteSpace(user.DisplayName)
                        ? user.Username
                        : user.DisplayName;

                clbMembers.Items.Add(
                    $"{displayName} (@{user.Username})");
            }
        }

        private void BtnCreate_Click(
            object? sender,
            EventArgs e)
        {
            string groupName = txtGroupName.Text.Trim();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên nhóm.",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtGroupName.Focus();
                return;
            }

            if (groupName.Contains('|'))
            {
                MessageBox.Show(
                    "Tên nhóm không được chứa ký tự |.",
                    "Tên nhóm không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            List<int> memberIds = clbMembers.CheckedIndices
                .Cast<int>()
                .Select(index =>
                    selectableUsers[index].UserId)
                .ToList();

            try
            {
                btnCreate.Enabled = false;

                groupService.RequestCreateGroup(
                    groupName,
                    memberIds);
            }
            catch (Exception ex)
            {
                btnCreate.Enabled = true;

                MessageBox.Show(
                    ex.Message,
                    "Không thể tạo nhóm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnGroupCreated(Group group)
        {
            RunOnUiThread(() =>
            {
                CreatedGroup = group;
                DialogResult = DialogResult.OK;
                Close();
            });
        }

        private void OnCreateGroupFailed(string errorMessage)
        {
            RunOnUiThread(() =>
            {
                btnCreate.Enabled = true;

                MessageBox.Show(
                    errorMessage,
                    "Tạo nhóm thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            });
        }

        private void CreateGroupForm_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            groupService.GroupCreated -= OnGroupCreated;
            groupService.CreateGroupFailed -= OnCreateGroupFailed;
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