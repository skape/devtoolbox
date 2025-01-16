using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevToolbox.Models;
using DevToolbox.Utils;

namespace DevToolbox.Forms
{
    public class SSHForm : Form
    {
        private ListView listViewSSH;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;

        public SSHForm()
        {
            InitializeUI();
            LoadSSHList();
        }

        private void InitializeUI()
        {
            this.Text = "SSH服务器管理";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建ListView
            listViewSSH = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new Point(10, 10),
                Size = new Size(760, 400)
            };

            // 添加列
            listViewSSH.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "名称", Width = 150 },
                new ColumnHeader { Text = "主机", Width = 200 },
                new ColumnHeader { Text = "端口", Width = 80 },
                new ColumnHeader { Text = "用户名", Width = 150 },
                new ColumnHeader { Text = "最后使用时间", Width = 150 }
            });

            // 创建按钮
            btnAdd = new Button
            {
                Text = "添加",
                Location = new Point(10, 420),
                Size = new Size(80, 30)
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "编辑",
                Location = new Point(100, 420),
                Size = new Size(80, 30)
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(190, 420),
                Size = new Size(80, 30)
            };
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(280, 420),
                Size = new Size(80, 30)
            };
            btnRefresh.Click += BtnRefresh_Click;

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                listViewSSH,
                btnAdd,
                btnEdit,
                btnDelete,
                btnRefresh
            });

            // 添加双击编辑事件
            listViewSSH.DoubleClick += (s, e) => BtnEdit_Click(s, e);
        }

        private void LoadSSHList()
        {
            try
            {
                var configs = ConfigManager.LoadSSHConfigs()
                    .OrderByDescending(c => c.LastUsed)
                    .ToList();

                listViewSSH.Items.Clear();
                foreach (var config in configs)
                {
                    var item = new ListViewItem(config.Name);
                    item.SubItems.AddRange(new string[]
                    {
                        config.Host,
                        config.Port.ToString(),
                        config.Username,
                        config.LastUsed.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                    item.Tag = config;
                    listViewSSH.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载SSH配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            ShowSSHConfigDialog(null);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (listViewSSH.SelectedItems.Count > 0)
            {
                var config = (SSHConfig)listViewSSH.SelectedItems[0].Tag;
                ShowSSHConfigDialog(config);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listViewSSH.SelectedItems.Count > 0)
            {
                var config = (SSHConfig)listViewSSH.SelectedItems[0].Tag;
                if (MessageBox.Show($"确定要删除服务器 {config.Name} 吗？", "确认删除",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ConfigManager.DeleteSSHConfig(config.Name);
                    LoadSSHList();
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadSSHList();
        }

        private void ShowSSHConfigDialog(SSHConfig config)
        {
            using (var form = new Form())
            {
                form.Text = config == null ? "添加SSH服务器" : "编辑SSH服务器";
                form.Size = new Size(400, 300);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                // 创建输入控件
                var lblName = new Label { Text = "名称:", Location = new Point(20, 20), AutoSize = true };
                var txtName = new TextBox
                {
                    Location = new Point(120, 20),
                    Size = new Size(200, 23),
                    Text = config?.Name ?? ""
                };

                var lblHost = new Label { Text = "主机:", Location = new Point(20, 50), AutoSize = true };
                var txtHost = new TextBox
                {
                    Location = new Point(120, 50),
                    Size = new Size(200, 23),
                    Text = config?.Host ?? ""
                };

                var lblPort = new Label { Text = "端口:", Location = new Point(20, 80), AutoSize = true };
                var txtPort = new TextBox
                {
                    Location = new Point(120, 80),
                    Size = new Size(200, 23),
                    Text = config?.Port.ToString() ?? "22"
                };

                var lblUsername = new Label { Text = "用户名:", Location = new Point(20, 110), AutoSize = true };
                var txtUsername = new TextBox
                {
                    Location = new Point(120, 110),
                    Size = new Size(200, 23),
                    Text = config?.Username ?? ""
                };

                var lblPassword = new Label { Text = "密码:", Location = new Point(20, 140), AutoSize = true };
                var txtPassword = new TextBox
                {
                    Location = new Point(120, 140),
                    Size = new Size(200, 23),
                    PasswordChar = '*',
                    Text = config?.Password ?? ""
                };

                var btnSave = new Button
                {
                    Text = "保存",
                    DialogResult = DialogResult.OK,
                    Location = new Point(120, 200),
                    Size = new Size(80, 30)
                };

                var btnCancel = new Button
                {
                    Text = "取消",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(220, 200),
                    Size = new Size(80, 30)
                };

                form.Controls.AddRange(new Control[]
                {
                    lblName, txtName,
                    lblHost, txtHost,
                    lblPort, txtPort,
                    lblUsername, txtUsername,
                    lblPassword, txtPassword,
                    btnSave, btnCancel
                });

                form.AcceptButton = btnSave;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var newConfig = new SSHConfig
                        {
                            Name = txtName.Text.Trim(),
                            Host = txtHost.Text.Trim(),
                            Port = int.Parse(txtPort.Text.Trim()),
                            Username = txtUsername.Text.Trim(),
                            Password = txtPassword.Text,
                            LastUsed = DateTime.Now
                        };

                        ConfigManager.SaveSSHConfig(newConfig);
                        LoadSSHList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存SSH配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
} 