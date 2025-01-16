using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Renci.SshNet;
using DevToolbox.Models;
using DevToolbox.Utils;

namespace DevToolbox.Forms
{
    public class SSHLoginForm : Form
    {
        private ComboBox cmbSavedConfigs;
        private TextBox txtHost;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtRemark;
        private Button btnConnect;
        private Button btnSave;
        private Button btnDelete;
        private CheckBox chkSaveConfig;

        public SSHLoginForm()
        {
            InitializeUI();
            LoadSavedConfigs();
        }

        private void InitializeUI()
        {
            this.Text = "SSH Login";
            this.Size = new System.Drawing.Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 保存的配置下拉框
            Label lblSavedConfigs = new Label
            {
                Text = "已保存的配置:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            cmbSavedConfigs = new ComboBox
            {
                Location = new Point(120, 20),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSavedConfigs.SelectedIndexChanged += CmbSavedConfigs_SelectedIndexChanged;

            // Host
            Label lblHost = new Label
            {
                Text = "Host:",
                Location = new Point(20, 60),
                AutoSize = true
            };

            txtHost = new TextBox
            {
                Location = new Point(120, 60),
                Size = new Size(200, 20)
            };

            // Username
            Label lblUsername = new Label
            {
                Text = "Username:",
                Location = new Point(20, 100),
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Location = new Point(120, 100),
                Size = new Size(200, 20)
            };

            // Password
            Label lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(20, 140),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(120, 140),
                Size = new Size(200, 20),
                PasswordChar = '*'
            };

            // Remark
            Label lblRemark = new Label
            {
                Text = "备注:",
                Location = new Point(20, 180),
                AutoSize = true
            };

            txtRemark = new TextBox
            {
                Location = new Point(120, 180),
                Size = new Size(200, 20)
            };

            // Save checkbox
            chkSaveConfig = new CheckBox
            {
                Text = "保存配置",
                Location = new Point(120, 210),
                AutoSize = true
            };

            // Connect Button
            btnConnect = new Button
            {
                Text = "连接",
                Location = new Point(120, 240),
                Size = new Size(100, 30)
            };
            btnConnect.Click += BtnConnect_Click;

            // Save Button
            btnSave = new Button
            {
                Text = "保存",
                Location = new Point(230, 240),
                Size = new Size(90, 30)
            };
            btnSave.Click += BtnSave_Click;

            // Delete Button
            btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(230, 280),
                Size = new Size(90, 30)
            };
            btnDelete.Click += BtnDelete_Click;

            // Add controls
            this.Controls.AddRange(new Control[] {
                lblSavedConfigs, cmbSavedConfigs,
                lblHost, txtHost,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblRemark, txtRemark,
                chkSaveConfig,
                btnConnect, btnSave, btnDelete
            });
        }

        private void LoadSavedConfigs()
        {
            var configs = ConfigManager.LoadConfigs()
                .OrderByDescending(c => c.LastUsed)
                .ToList();

            cmbSavedConfigs.Items.Clear();
            cmbSavedConfigs.Items.Add("-- 新建连接 --");
            foreach (var config in configs)
            {
                cmbSavedConfigs.Items.Add(config.Remark);
            }

            cmbSavedConfigs.SelectedIndex = 0;
        }

        private void CmbSavedConfigs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSavedConfigs.SelectedIndex == 0)
            {
                ClearFields();
                return;
            }

            var configs = ConfigManager.LoadConfigs()
                .OrderByDescending(c => c.LastUsed)
                .ToList();

            var selectedConfig = configs[cmbSavedConfigs.SelectedIndex - 1];
            txtHost.Text = selectedConfig.Host;
            txtUsername.Text = selectedConfig.Username;
            txtPassword.Text = selectedConfig.Password;
            txtRemark.Text = selectedConfig.Remark;
        }

        private void ClearFields()
        {
            txtHost.Text = "";
            txtUsername.Text = "";
            txtPassword.Text = "";
            txtRemark.Text = "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text) || 
                string.IsNullOrWhiteSpace(txtUsername.Text) || 
                string.IsNullOrWhiteSpace(txtRemark.Text))
            {
                MessageBox.Show("请填写完整信息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var config = new SSHConfig
            {
                Host = txtHost.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                Remark = txtRemark.Text
            };

            ConfigManager.SaveConfig(config);
            LoadSavedConfigs();
            MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (cmbSavedConfigs.SelectedIndex <= 0)
            {
                return;
            }

            if (MessageBox.Show("确定要删除这个配置吗？", "确认", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var configs = ConfigManager.LoadConfigs()
                    .OrderByDescending(c => c.LastUsed)
                    .ToList();
                configs.RemoveAt(cmbSavedConfigs.SelectedIndex - 1);
                ConfigManager.SaveConfigs(configs);
                LoadSavedConfigs();
            }
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                using (var client = new SshClient(txtHost.Text, txtUsername.Text, txtPassword.Text))
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        if (chkSaveConfig.Checked)
                        {
                            var config = new SSHConfig
                            {
                                Host = txtHost.Text,
                                Username = txtUsername.Text,
                                Password = txtPassword.Text,
                                Remark = txtRemark.Text
                            };
                            ConfigManager.SaveConfig(config);
                        }

                        DockerForm dockerForm = new DockerForm(client);
                        this.Hide();
                        dockerForm.ShowDialog();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 