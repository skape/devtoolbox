using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevToolbox.Models;
using DevToolbox.Data;

namespace DevToolbox.Forms
{
    public class SSHForm : Form
    {
        private ListView listViewServers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnClose;
        private readonly SSHConfigRepository _repository;

        public SSHForm()
        {
            _repository = new SSHConfigRepository();
            InitializeUI();
            LoadServerList();
        }

        private void InitializeUI()
        {
            this.Text = "SSH服务器管理";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 创建ListView
            listViewServers = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new Point(20, 20),
                Size = new Size(740, 380)
            };

            // 添加列
            listViewServers.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "名称", Width = 120 },
                new ColumnHeader { Text = "主机", Width = 200 },
                new ColumnHeader { Text = "端口", Width = 80 },
                new ColumnHeader { Text = "用户名", Width = 120 },
                new ColumnHeader { Text = "最后使用时间", Width = 200 }
            });

            // 创建按钮
            btnAdd = new Button
            {
                Text = "添加",
                Location = new Point(20, 420),
                Size = new Size(80, 30)
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "编辑",
                Location = new Point(110, 420),
                Size = new Size(80, 30),
                Enabled = false
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(200, 420),
                Size = new Size(80, 30),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(680, 420),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                listViewServers,
                btnAdd,
                btnEdit,
                btnDelete,
                btnClose
            });

            // 添加选择变更事件
            listViewServers.SelectedIndexChanged += (s, e) =>
            {
                bool hasSelection = listViewServers.SelectedItems.Count > 0;
                btnEdit.Enabled = hasSelection;
                btnDelete.Enabled = hasSelection;
            };

            this.CancelButton = btnClose;
        }

        private void LoadServerList()
        {
            listViewServers.Items.Clear();
            var configs = _repository.GetAll();

            foreach (var config in configs)
            {
                var item = new ListViewItem(new[]
                {
                    config.Name,
                    config.Host,
                    config.Port.ToString(),
                    config.Username,
                    config.LastUsed?.ToString() ?? ""
                });
                item.Tag = config;
                listViewServers.Items.Add(item);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var form = new SSHConfigForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _repository.Add(form.Config);
                    LoadServerList();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (listViewServers.SelectedItems.Count == 0) return;

            var config = (SSHConfig)listViewServers.SelectedItems[0].Tag;
            using (var form = new SSHConfigForm(config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _repository.Update(form.Config);
                    LoadServerList();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listViewServers.SelectedItems.Count == 0) return;

            var config = (SSHConfig)listViewServers.SelectedItems[0].Tag;
            if (MessageBox.Show(
                $"确定要删除服务器 {config.Name} 吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _repository.Delete(config.Id);
                LoadServerList();
            }
        }
    }
} 