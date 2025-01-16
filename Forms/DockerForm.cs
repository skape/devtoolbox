using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Renci.SshNet;

namespace DevToolbox.Forms
{
    public class DockerForm : Form
    {
        private ListView listViewContainers;
        private Button btnRefresh;
        private SshClient sshClient;
        private ContextMenuStrip containerContextMenu;

        public DockerForm(SshClient client)
        {
            sshClient = client;
            InitializeUI();
            LoadContainers();
        }

        private void InitializeUI()
        {
            this.Text = "Docker Containers";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 创建ListView
            listViewContainers = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(760, 500)
            };

            // 添加列
            listViewContainers.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "Container ID", Width = 100 },
                new ColumnHeader { Text = "Image", Width = 150 },
                new ColumnHeader { Text = "Command", Width = 150 },
                new ColumnHeader { Text = "Created", Width = 100 },
                new ColumnHeader { Text = "Status", Width = 100 },
                new ColumnHeader { Text = "Ports", Width = 150 }
            });

            // 刷新按钮
            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new System.Drawing.Point(10, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            btnRefresh.Click += BtnRefresh_Click;

            // 添加容器操作按钮
            Button btnStart = new Button
            {
                Text = "启动",
                Location = new System.Drawing.Point(120, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            btnStart.Click += BtnStart_Click;

            Button btnStop = new Button
            {
                Text = "停止",
                Location = new System.Drawing.Point(230, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            btnStop.Click += BtnStop_Click;

            Button btnRestart = new Button
            {
                Text = "重启",
                Location = new System.Drawing.Point(340, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            btnRestart.Click += BtnRestart_Click;

            Button btnLogs = new Button
            {
                Text = "查看日志",
                Location = new System.Drawing.Point(450, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            btnLogs.Click += BtnLogs_Click;

            // 添加控件
            this.Controls.AddRange(new Control[] { 
                listViewContainers, 
                btnRefresh, 
                btnStart, 
                btnStop, 
                btnRestart, 
                btnLogs 
            });

            // 初始化右键菜单
            InitializeContextMenu();
            
            // 为ListView添加右键菜单
            listViewContainers.ContextMenuStrip = containerContextMenu;

            // 添加双击事件查看日志
            listViewContainers.DoubleClick += (s, e) => ShowContainerLogs(GetSelectedContainerId());
        }

        private void InitializeContextMenu()
        {
            containerContextMenu = new ContextMenuStrip();
            
            // 添加菜单项
            var startItem = new ToolStripMenuItem("启动", null, (s, e) => {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker start {containerId}", "启动容器");
                    LoadContainers();
                }
            });

            var stopItem = new ToolStripMenuItem("停止", null, (s, e) => {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker stop {containerId}", "停止容器");
                    LoadContainers();
                }
            });

            var restartItem = new ToolStripMenuItem("重启", null, (s, e) => {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker restart {containerId}", "重启容器");
                    LoadContainers();
                }
            });

            var logsItem = new ToolStripMenuItem("查看日志", null, (s, e) => {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ShowContainerLogs(containerId);
                }
            });

            var inspectItem = new ToolStripMenuItem("查看详细信息", null, (s, e) => {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ShowContainerInspect(containerId);
                }
            });

            // 添加分隔线和菜单项
            containerContextMenu.Items.AddRange(new ToolStripItem[] {
                startItem,
                stopItem,
                restartItem,
                new ToolStripSeparator(),
                logsItem,
                inspectItem
            });

            // 在显示菜单前检查容器状态并启用/禁用相应选项
            containerContextMenu.Opening += (s, e) => {
                string status = GetSelectedContainerStatus();
                startItem.Enabled = status?.Contains("Exited") ?? false;
                stopItem.Enabled = status?.Contains("Up") ?? false;
                restartItem.Enabled = status?.Contains("Up") ?? false;
            };
        }

        private string GetSelectedContainerId()
        {
            return listViewContainers.SelectedItems.Count > 0 
                ? listViewContainers.SelectedItems[0].Text 
                : null;
        }

        private string GetSelectedContainerStatus()
        {
            return listViewContainers.SelectedItems.Count > 0 
                ? listViewContainers.SelectedItems[0].SubItems[4].Text 
                : null;
        }

        private void LoadContainers()
        {
            try
            {
                var command = sshClient.CreateCommand("docker ps -a --format \"{{.ID}}\\t{{.Image}}\\t{{.Command}}\\t{{.CreatedAt}}\\t{{.Status}}\\t{{.Ports}}\"");
                var result = command.Execute();

                listViewContainers.Items.Clear();
                foreach (var line in result.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('\t');
                    if (parts.Length >= 6)
                    {
                        var item = new ListViewItem(parts[0]); // Container ID
                        for (int i = 1; i < 6; i++)
                        {
                            item.SubItems.Add(parts[i]);
                        }

                        // 根据容器状态设置不同的颜色
                        if (parts[4].Contains("Up"))
                        {
                            item.ForeColor = Color.Green; // 运行中的容器显示为绿色
                        }
                        else if (parts[4].Contains("Exited"))
                        {
                            item.ForeColor = Color.Red; // 已停止的容器显示为红色
                        }

                        listViewContainers.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取容器列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadContainers();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (listViewContainers.SelectedItems.Count > 0)
            {
                string containerId = listViewContainers.SelectedItems[0].Text;
                ExecuteDockerCommand($"docker start {containerId}", "启动容器");
                LoadContainers(); // 刷新列表
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (listViewContainers.SelectedItems.Count > 0)
            {
                string containerId = listViewContainers.SelectedItems[0].Text;
                ExecuteDockerCommand($"docker stop {containerId}", "停止容器");
                LoadContainers(); // 刷新列表
            }
        }

        private void BtnRestart_Click(object sender, EventArgs e)
        {
            if (listViewContainers.SelectedItems.Count > 0)
            {
                string containerId = listViewContainers.SelectedItems[0].Text;
                ExecuteDockerCommand($"docker restart {containerId}", "重启容器");
                LoadContainers(); // 刷新列表
            }
        }

        private void BtnLogs_Click(object sender, EventArgs e)
        {
            if (listViewContainers.SelectedItems.Count > 0)
            {
                string containerId = listViewContainers.SelectedItems[0].Text;
                ShowContainerLogs(containerId);
            }
        }

        private void ExecuteDockerCommand(string command, string operation)
        {
            try
            {
                var cmd = sshClient.CreateCommand(command);
                var result = cmd.Execute();
                if (string.IsNullOrEmpty(cmd.Error))
                {
                    MessageBox.Show($"{operation}成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"{operation}失败: {cmd.Error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{operation}失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowContainerLogs(string containerId)
        {
            try
            {
                // 创建新的日志窗体
                Form logsForm = new Form
                {
                    Text = $"Container Logs - {containerId}",
                    Size = new System.Drawing.Size(1000, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                RichTextBox txtLogs = new RichTextBox
                {
                    Multiline = true,
                    ScrollBars = RichTextBoxScrollBars.Both,
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Font = new System.Drawing.Font("Consolas", 10F),
                    BackColor = Color.Black,
                    ForeColor = Color.LightGray
                };

                // 添加工具栏
                ToolStrip toolStrip = new ToolStrip();
                
                var refreshButton = new ToolStripButton("刷新");
                refreshButton.Click += (s, e) => LoadLogs(containerId, txtLogs);

                var followButton = new ToolStripButton("实时跟踪");
                var followTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                followTimer.Tick += (s, e) => LoadLogs(containerId, txtLogs, true);
                
                followButton.Click += (s, e) => {
                    followButton.Checked = !followButton.Checked;
                    followTimer.Enabled = followButton.Checked;
                };

                var clearButton = new ToolStripButton("清除");
                clearButton.Click += (s, e) => txtLogs.Clear();

                toolStrip.Items.AddRange(new ToolStripItem[] {
                    refreshButton,
                    followButton,
                    new ToolStripSeparator(),
                    clearButton
                });

                logsForm.Controls.AddRange(new Control[] { toolStrip, txtLogs });

                // 初始加载日志
                LoadLogs(containerId, txtLogs);

                // 窗体关闭时停止计时器
                logsForm.FormClosing += (s, e) => followTimer.Stop();

                logsForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取日志失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLogs(string containerId, RichTextBox txtLogs, bool append = false)
        {
            try
            {
                var command = sshClient.CreateCommand($"docker logs --tail 1000 {containerId}");
                var logs = command.Execute();

                if (!append)
                {
                    txtLogs.Text = logs;
                }
                else
                {
                    txtLogs.AppendText(logs);
                }
                txtLogs.SelectionStart = txtLogs.TextLength;
                txtLogs.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取日志失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowContainerInspect(string containerId)
        {
            try
            {
                var command = sshClient.CreateCommand($"docker inspect {containerId}");
                var info = command.Execute();

                Form inspectForm = new Form
                {
                    Text = $"Container Inspect - {containerId}",
                    Size = new System.Drawing.Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                RichTextBox txtInfo = new RichTextBox
                {
                    Multiline = true,
                    ScrollBars = RichTextBoxScrollBars.Both,
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Font = new System.Drawing.Font("Consolas", 10F),
                    Text = info
                };

                inspectForm.Controls.Add(txtInfo);
                inspectForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取容器信息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (sshClient != null && sshClient.IsConnected)
            {
                sshClient.Disconnect();
                sshClient.Dispose();
            }
        }
    }
} 