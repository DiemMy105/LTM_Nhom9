using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChatTCP.Client.Services;
using ChatTCP.Shared.Models;

namespace ChatTCP.Client.Forms
{
    public class GroupMembersForm : Form
    {
        private readonly GroupService groupService;
        private readonly List<User> allUsers;
        private Group group;

        private readonly Label lblRole = new Label();
        private readonly DataGridView gridMembers = new DataGridView();
        private readonly ComboBox cboUsers = new ComboBox();
        private readonly Button btnAdd = new Button();
        private readonly Button btnDissolve = new Button();

        private bool IsOwner =>
            group.CreatedBy == groupService.CurrentUserId;

        public GroupMembersForm(
            Group group,
            IEnumerable<User> users,
            GroupService groupService)
        {
            this.group = group;
            this.groupService = groupService;
            allUsers = users
                .Where(user => user.UserId > 0)
                .GroupBy(user => user.UserId)
                .Select(items => items.First())
                .ToList();

            InitializeUi();
            RefreshUi();

            groupService.GroupUpdated += OnGroupUpdated;
            groupService.GroupDissolved += OnGroupDissolved;
            groupService.GroupManagementFailed += OnFailed;
            FormClosed += OnFormClosed;
        }

        private void InitializeUi()
        {
            Text = $"Thành viên - {group.GroupName}";
            Size = new Size(460, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            Label title = new Label
            {
                Text = "Danh sách thành viên",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };

            lblRole.Location = new Point(20, 45);
            lblRole.AutoSize = true;

            gridMembers.Location = new Point(20, 75);
            gridMembers.Size = new Size(405, 290);
            gridMembers.AllowUserToAddRows = false;
            gridMembers.AllowUserToDeleteRows = false;
            gridMembers.ReadOnly = true;
            gridMembers.RowHeadersVisible = false;
            gridMembers.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            gridMembers.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            gridMembers.CellContentClick += GridMembers_CellContentClick;

            gridMembers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "MemberName",
                    HeaderText = "Thành viên",
                    FillWeight = 75
                });

            gridMembers.Columns.Add(
                new DataGridViewButtonColumn
                {
                    Name = "DeleteMember",
                    HeaderText = "",
                    Text = "Xóa",
                    UseColumnTextForButtonValue = true,
                    FillWeight = 25
                });

            cboUsers.Location = new Point(20, 385);
            cboUsers.Size = new Size(300, 28);
            cboUsers.DropDownStyle = ComboBoxStyle.DropDownList;

            btnAdd.Text = "Thêm";
            btnAdd.Location = new Point(330, 384);
            btnAdd.Size = new Size(95, 30);
            btnAdd.Click += BtnAdd_Click;

            btnDissolve.Text = "Giải tán nhóm";
            btnDissolve.Location = new Point(285, 435);
            btnDissolve.Size = new Size(140, 34);
            btnDissolve.BackColor = Color.IndianRed;
            btnDissolve.ForeColor = Color.White;
            btnDissolve.FlatStyle = FlatStyle.Flat;
            btnDissolve.Click += BtnDissolve_Click;

            Controls.Add(title);
            Controls.Add(lblRole);
            Controls.Add(gridMembers);
            Controls.Add(cboUsers);
            Controls.Add(btnAdd);
            Controls.Add(btnDissolve);
        }

        private void RefreshUi()
        {
            lblRole.Text = IsOwner
                ? "Bạn là nhóm trưởng"
                : "Bạn là thành viên";

            cboUsers.Visible = IsOwner;
            btnAdd.Visible = IsOwner;
            btnDissolve.Visible = IsOwner;
            gridMembers.Columns["DeleteMember"].Visible = IsOwner;

            gridMembers.Rows.Clear();

            foreach (int memberId in group.MemberIds)
            {
                string name = GetUserName(memberId);
                string text = memberId == group.CreatedBy
                    ? $"{name} - Nhóm trưởng"
                    : name;

                int rowIndex = gridMembers.Rows.Add(text);
                gridMembers.Rows[rowIndex].Tag = memberId;

                if (memberId == group.CreatedBy)
                {
                    int deleteColumnIndex =
                        gridMembers.Columns["DeleteMember"].Index;

                    gridMembers.Rows[rowIndex].Cells[deleteColumnIndex] =
                        new DataGridViewTextBoxCell();
                }
            }

            cboUsers.Items.Clear();

            foreach (User user in allUsers.Where(
                user => !group.MemberIds.Contains(user.UserId)))
            {
                cboUsers.Items.Add(new UserOption(user));
            }

            if (cboUsers.Items.Count > 0)
            {
                cboUsers.SelectedIndex = 0;
            }
        }

        private string GetUserName(int userId)
        {
            User? user = allUsers.FirstOrDefault(
                item => item.UserId == userId);

            if (user == null)
            {
                return $"Thành viên #{userId}";
            }

            return string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Username
                : user.DisplayName;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (cboUsers.SelectedItem is not UserOption option)
            {
                MessageBox.Show("Không còn người dùng để thêm.");
                return;
            }

            TryRun(() => groupService.RequestAddMember(
                group.GroupId,
                option.User.UserId));
        }

        private void GridMembers_CellContentClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            if (!IsOwner ||
                e.RowIndex < 0 ||
                e.ColumnIndex !=
                    gridMembers.Columns["DeleteMember"].Index)
            {
                return;
            }

            int memberId =
                (int)gridMembers.Rows[e.RowIndex].Tag;
            string name = GetUserName(memberId);

            DialogResult answer = MessageBox.Show(
                $"Bạn có muốn xóa thành viên {name}?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Yes)
            {
                TryRun(() => groupService.RequestRemoveMember(
                    group.GroupId,
                    memberId));
            }
        }

        private void BtnDissolve_Click(object? sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                "Bạn có muốn giải tán nhóm này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Yes)
            {
                TryRun(() => groupService.RequestDissolveGroup(
                    group.GroupId));
            }
        }

        private void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Quản lý nhóm thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnGroupUpdated(Group updatedGroup)
        {
            if (updatedGroup.GroupId != group.GroupId) return;

            RunOnUiThread(() =>
            {
                group = updatedGroup;

                if (!group.MemberIds.Contains(
                    groupService.CurrentUserId))
                {
                    MessageBox.Show("Bạn đã bị xóa khỏi nhóm.");
                    Close();
                    return;
                }

                RefreshUi();
            });
        }

        private void OnGroupDissolved(int groupId)
        {
            if (groupId != group.GroupId) return;

            RunOnUiThread(() =>
            {
                MessageBox.Show("Nhóm đã được giải tán.");
                Close();
            });
        }

        private void OnFailed(string message)
        {
            RunOnUiThread(() => MessageBox.Show(
                message,
                "Quản lý nhóm thất bại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error));
        }

        private void OnFormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            groupService.GroupUpdated -= OnGroupUpdated;
            groupService.GroupDissolved -= OnGroupDissolved;
            groupService.GroupManagementFailed -= OnFailed;
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

        private class UserOption
        {
            public User User { get; }

            public UserOption(User user)
            {
                User = user;
            }

            public override string ToString()
            {
                string name =
                    string.IsNullOrWhiteSpace(User.DisplayName)
                        ? User.Username
                        : User.DisplayName;

                return $"{name} (@{User.Username})";
            }
        }
    }
}
