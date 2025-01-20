using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevToolbox.Models;
using DevToolbox.Utils;
using Renci.SshNet;

namespace DevToolbox.Forms
{
    public class SSHLoginForm : Form
    {
        private ComboBox cboServers;
        private Button btnConnect;
        private Button btnManage;
        private Button btnCancel;

        public SshClient SshClient { get; private set; }

        public SSHLoginForm()
        {
            InitializeUI();
            LoadServerList();
        }

        private void InitializeUI()
        {
            this.Text = "SSH登录";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblServer = new Label
            {
                Text = "选择服务器:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            cboServers = new ComboBox
            {
                Location = new Point(20, 45),
                Size = new Size(340, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnConnect = new Button
            {
                Text = "连接",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 90),
                Size = new Size(100, 30)
            };

            btnManage = new Button
            {
                Text = "管理服务器",
                Location = new Point(140, 90),
                Size = new Size(100, 30)
            };
            btnManage.Click += BtnManage_Click;

            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(260, 90),
                Size = new Size(100, 30)
            };

            this.Controls.AddRange(new Control[]
            {
                lblServer,
                cboServers,
                btnConnect,
                btnManage,
                btnCancel
            });

            this.AcceptButton = btnConnect;
            this.CancelButton = btnCancel;
        }

        private void LoadServerList()
        {
            try
            {
                var configs = ConfigManager.LoadSSHConfigs()
                    .OrderByDescending(c => c.LastUsed)
                    .ToList();

                cboServers.Items.Clear();
                foreach (var config in configs)
                {
                    cboServers.Items.Add(new SSHConfigItem(config));
                }

                if (cboServers.Items.Count > 0)
                {
                    cboServers.SelectedIndex = 0;
                }

                // 根据是否有可用服务器启用/禁用连接按钮
                btnConnect.Enabled = cboServers.Items.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载SSH配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnManage_Click(object sender, EventArgs e)
        {
            using (var form = new SSHForm())
            {
                form.ShowDialog();
                LoadServerList(); // 刷新服务器列表
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (DialogResult == DialogResult.OK)
            {
                try
                {
                    if (cboServers.SelectedItem == null)
                    {
                        MessageBox.Show("请选择服务器", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Cancel = true;
                        return;
                    }

                    var config = ((SSHConfigItem)cboServers.SelectedItem).Config;
                    
                    // 显示连接中的加载窗口
                    using (var loadingForm = new LoadingForm(this, $"正在连接到 {config.Name} ({config.Host})..."))
                    {
                        loadingForm.Show();
                        Application.DoEvents();

                        try
                        {
                            // 更新最后使用时间
                            config.LastUsed = DateTime.Now;
                            ConfigManager.SaveSSHConfig(config);

                            // 创建SSH连接
                            SshClient = new SshClient(config.Host, config.Port, config.Username, config.Password);
                            SshClient.Connect();

                            // 连接成功，显示Docker容器列表
                            loadingForm.Close();
                            
                            // 创建并显示Docker窗口
                            var dockerForm = new DockerForm(SshClient);
                            dockerForm.ShowDialog();
                            LoadServerList(); // 刷新服务器列表

                            // Docker窗口关闭后，断开SSH连接
                            try
                            {
                                if (SshClient != null && SshClient.IsConnected)
                                {
                                    SshClient.Disconnect();
                                }
                            }
                            finally
                            {
                                if (SshClient != null)
                                {
                                    SshClient.Dispose();
                                    SshClient = null;
                                }
                            }

                            // 取消窗口关闭，保持SSH登录窗口打开
                            e.Cancel = true;
                        }
                        catch (Exception ex)
                        {
                            loadingForm.Close();
                            MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            try
                            {
                                if (SshClient != null && SshClient.IsConnected)
                                {
                                    SshClient.Disconnect();
                                }
                            }
                            finally
                            {
                                if (SshClient != null)
                                {
                                    SshClient.Dispose();
                                    SshClient = null;
                                }
                            }
                            e.Cancel = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"连接过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    try
                    {
                        if (SshClient != null && SshClient.IsConnected)
                        {
                            SshClient.Disconnect();
                        }
                    }
                    finally
                    {
                        if (SshClient != null)
                        {
                            SshClient.Dispose();
                            SshClient = null;
                        }
                    }
                    e.Cancel = true;
                }
            }
        }

        private class SSHConfigItem
        {
            public SSHConfig Config { get; }

            public SSHConfigItem(SSHConfig config)
            {
                Config = config;
            }

            public override string ToString()
            {
                return $"{Config.Name} ({Config.Host}:{Config.Port})";
            }
        }
    }
} 