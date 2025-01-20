using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Renci.SshNet;
using System.Linq;
using System.Threading.Tasks;
using DevToolbox.Models;
using DevToolbox.Utils;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;

namespace DevToolbox.Forms
{
    // 添加扩展方法类
    public static class ControlExtensions
    {
        public static async Task InvokeAsync(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                await Task.Run(() => control.Invoke(action));
            }
            else
            {
                action();
            }
        }
    }

    public class DockerForm : Form
    {
        private ListView listViewContainers;
        private Button btnRefresh;
        private SshClient sshClient;
        private ContextMenuStrip containerContextMenu;

        public DockerForm(SshClient client)
        {
            sshClient = client;
            
            // 获取连接信息
            var connectionInfo = sshClient.ConnectionInfo;
            var host = connectionInfo.Host;
            var port = connectionInfo.Port;
            var username = connectionInfo.Username;

            // 从已保存的配置中查找当前连接的服务器名称
            var configs = ConfigManager.LoadSSHConfigs();
            var currentConfig = configs.FirstOrDefault(c => 
                c.Host == host && 
                c.Port == port && 
                c.Username == username);

            // 设置窗口标题
            this.Text = currentConfig != null
                ? $"Docker Containers - {currentConfig.Name} ({host}:{port})"
                : $"Docker Containers - {host}:{port}";

            InitializeUI();
            LoadContainers();
        }

        private void InitializeUI()
        {
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

            Button batchOpertaionBtn = new Button
            {
                Text = "批量操作",
                Location = new System.Drawing.Point(650, 520),
                Size = new System.Drawing.Size(100, 30)
            };
            batchOpertaionBtn.Click += BtnBatchOperation_Click;

            // 添加控件
            this.Controls.AddRange(new Control[] {
                listViewContainers,
                btnRefresh,
                btnStart,
                btnStop,
                btnRestart,
                btnLogs,
                batchOpertaionBtn
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
            var startItem = new ToolStripMenuItem("启动", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker start {containerId}", "启动容器");
                    LoadContainers();
                }
            });

            var stopItem = new ToolStripMenuItem("停止", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker stop {containerId}", "停止容器");
                    LoadContainers();
                }
            });

            var restartItem = new ToolStripMenuItem("重启", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteDockerCommand($"docker restart {containerId}", "重启容器");
                    LoadContainers();
                }
            });

            var logsItem = new ToolStripMenuItem("查看日志", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ShowContainerLogs(containerId);
                }
            });

            var inspectItem = new ToolStripMenuItem("查看详细信息", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ShowContainerInspect(containerId);
                }
            });

            var buildDevItem = new ToolStripMenuItem("打包Dev", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteBuild(containerId, "staging");
                }
            });

            var buildProdItem = new ToolStripMenuItem("打包Prod", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    ExecuteBuild(containerId, "prod");
                }
            });

            var deployDevItem = new ToolStripMenuItem("开发环境部署", null, async (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    await Deploy(containerId, "dev");
                }
            });

            var deployProdItem = new ToolStripMenuItem("生产环境部署", null, async (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    await Deploy(containerId, "prod");
                }
            });

            var buildAndDeployDevItem = new ToolStripMenuItem("开发环境打包及部署", null, async (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    // 先执行打包
                    var buildCommand = $"docker exec {containerId} yarn build:staging";
                    var cmd = sshClient.CreateCommand(buildCommand);
                    
                    // 创建日志窗口
                    Form logForm = new Form
                    {
                        Text = $"Build Logs - staging",
                        Size = new System.Drawing.Size(800, 600),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    RichTextBox logBox = new RichTextBox
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        BackColor = Color.Black,
                        ForeColor = Color.LightGreen,
                        Font = new Font("Consolas", 10F),
                        Multiline = true,
                        ScrollBars = RichTextBoxScrollBars.Both
                    };

                    logForm.Controls.Add(logBox);
                    logForm.Show();

                    try 
                    {
                        var asyncResult = cmd.BeginExecute();
                        
                        using (var reader = new StreamReader(cmd.OutputStream))
                        using (var errorReader = new StreamReader(cmd.ExtendedOutputStream))
                        {
                            while (!asyncResult.IsCompleted || !reader.EndOfStream || !errorReader.EndOfStream)
                            {
                                if (!reader.EndOfStream)
                                {
                                    string line = await reader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        await logBox.InvokeAsync(() =>
                                        {
                                            logBox.AppendText(line + Environment.NewLine);
                                            logBox.ScrollToCaret();
                                        });
                                    }
                                }

                                if (!errorReader.EndOfStream)
                                {
                                    string errorLine = await errorReader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(errorLine))
                                    {
                                        await logBox.InvokeAsync(() =>
                                        {
                                            logBox.SelectionColor = Color.Red;
                                            logBox.AppendText(errorLine + Environment.NewLine);
                                            logBox.SelectionColor = logBox.ForeColor;
                                            logBox.ScrollToCaret();
                                        });
                                    }
                                }

                                await Task.Delay(100);
                            }
                        }

                        cmd.EndExecute(asyncResult);

                        if (cmd.ExitStatus == 0)
                        {
                            await logBox.InvokeAsync(() =>
                            {
                                logBox.SelectionColor = Color.Green;
                                logBox.AppendText("\n\n构建成功完成！\n");
                                logBox.SelectionColor = logBox.ForeColor;
                            });
                            
                            // 关闭日志窗口
                            await logForm.InvokeAsync(() => logForm.Close());
                            
                            // 打包成功后，直接调用部署方法
                            await DeployDev(containerId);
                        }
                        else
                        {
                            await logBox.InvokeAsync(() =>
                            {
                                logBox.SelectionColor = Color.Red;
                                logBox.AppendText($"\n\n构建失败，退出代码: {cmd.ExitStatus}\n");
                                logBox.SelectionColor = logBox.ForeColor;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await logBox.InvokeAsync(() =>
                        {
                            logBox.SelectionColor = Color.Red;
                            logBox.AppendText($"\n\n执行命令时发生错误: {ex.Message}\n");
                            logBox.SelectionColor = logBox.ForeColor;
                        });
                    }
                }
            });

            var buildAndDeployProdItem = new ToolStripMenuItem("生产环境打包及部署", null, async (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    // 先执行打包
                    var buildCommand = $"docker exec {containerId} yarn build:prod";
                    var cmd = sshClient.CreateCommand(buildCommand);
                    
                    // 创建日志窗口
                    Form logForm = new Form
                    {
                        Text = $"Build Logs - prod",
                        Size = new System.Drawing.Size(800, 600),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    RichTextBox logBox = new RichTextBox
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        BackColor = Color.Black,
                        ForeColor = Color.LightGreen,
                        Font = new Font("Consolas", 10F),
                        Multiline = true,
                        ScrollBars = RichTextBoxScrollBars.Both
                    };

                    logForm.Controls.Add(logBox);
                    logForm.Show();

                    try 
                    {
                        var asyncResult = cmd.BeginExecute();
                        
                        using (var reader = new StreamReader(cmd.OutputStream))
                        using (var errorReader = new StreamReader(cmd.ExtendedOutputStream))
                        {
                            while (!asyncResult.IsCompleted || !reader.EndOfStream || !errorReader.EndOfStream)
                            {
                                if (!reader.EndOfStream)
                                {
                                    string line = await reader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        await logBox.InvokeAsync(() =>
                                        {
                                            logBox.AppendText(line + Environment.NewLine);
                                            logBox.ScrollToCaret();
                                        });
                                    }
                                }

                                if (!errorReader.EndOfStream)
                                {
                                    string errorLine = await errorReader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(errorLine))
                                    {
                                        await logBox.InvokeAsync(() =>
                                        {
                                            logBox.SelectionColor = Color.Red;
                                            logBox.AppendText(errorLine + Environment.NewLine);
                                            logBox.SelectionColor = logBox.ForeColor;
                                            logBox.ScrollToCaret();
                                        });
                                    }
                                }

                                await Task.Delay(100);
                            }
                        }

                        cmd.EndExecute(asyncResult);

                        if (cmd.ExitStatus == 0)
                        {
                            await logBox.InvokeAsync(() =>
                            {
                                logBox.SelectionColor = Color.Green;
                                logBox.AppendText("\n\n构建成功完成！\n");
                                logBox.SelectionColor = logBox.ForeColor;
                            });
                            
                            // 关闭日志窗口
                            await logForm.InvokeAsync(() => logForm.Close());
                            
                            // 打包成功后，直接调用部署方法
                            await DeployProd(containerId);
                        }
                        else
                        {
                            await logBox.InvokeAsync(() =>
                            {
                                logBox.SelectionColor = Color.Red;
                                logBox.AppendText($"\n\n构建失败，退出代码: {cmd.ExitStatus}\n");
                                logBox.SelectionColor = logBox.ForeColor;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await logBox.InvokeAsync(() =>
                        {
                            logBox.SelectionColor = Color.Red;
                            logBox.AppendText($"\n\n执行命令时发生错误: {ex.Message}\n");
                            logBox.SelectionColor = logBox.ForeColor;
                        });
                    }
                }
            });

            var downloadDBItem = new ToolStripMenuItem("下载数据库", null, (s, e) =>
            {
                string containerId = GetSelectedContainerId();
                if (!string.IsNullOrEmpty(containerId))
                {
                    DownloadDatabase(containerId);
                }
            });

            // 添加分隔线和菜单项
            containerContextMenu.Items.AddRange(new ToolStripItem[] {
                startItem,
                stopItem,
                restartItem,
                new ToolStripSeparator(),
                buildDevItem,
                buildProdItem,
                new ToolStripSeparator(),
                deployDevItem,
                deployProdItem,
                new ToolStripSeparator(),
                buildAndDeployDevItem,
                buildAndDeployProdItem,
                new ToolStripSeparator(),
                logsItem,
                inspectItem,
                downloadDBItem
            });

            // 在显示菜单前检查容器状态并启用/禁用相应选项
            containerContextMenu.Opening += (s, e) =>
            {
                string status = GetSelectedContainerStatus();
                string image = GetSelectedContainerImage()?.ToLower() ?? "";
                
                startItem.Enabled = status?.Contains("Exited") ?? false;
                stopItem.Enabled = status?.Contains("Up") ?? false;
                restartItem.Enabled = status?.Contains("Up") ?? false;
                downloadDBItem.Visible = image.Contains("mysql");
                
                // 只有运行中的容器才能执行打包和部署
                buildDevItem.Enabled = status?.Contains("Up") ?? false;
                buildProdItem.Enabled = status?.Contains("Up") ?? false;
                deployDevItem.Enabled = status?.Contains("Up") ?? false;
                deployProdItem.Enabled = status?.Contains("Up") ?? false;
                buildAndDeployDevItem.Enabled = status?.Contains("Up") ?? false;
                buildAndDeployProdItem.Enabled = status?.Contains("Up") ?? false;
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

        private string GetSelectedContainerImage()
        {
            return listViewContainers.SelectedItems.Count > 0
                ? listViewContainers.SelectedItems[0].SubItems[1].Text
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

                followButton.Click += (s, e) =>
                {
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

        private async void DownloadDatabase(string containerId)
        {
            var configs = ConfigManager.LoadMySQLConfigs();
            MySQLConfig lastConfig = null;

            // 获取上次的配置（如果有）
            configs.TryGetValue(containerId, out lastConfig);

            // 显示登录窗口
            using (var loginForm = new Form())
            {
                loginForm.Text = "MySQL登录";
                loginForm.Size = new Size(300, 200);
                loginForm.StartPosition = FormStartPosition.CenterParent;
                loginForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                loginForm.MaximizeBox = false;
                loginForm.MinimizeBox = false;

                var lblUsername = new Label { Text = "用户名:", Location = new Point(20, 20), AutoSize = true };
                var txtUsername = new TextBox
                {
                    Location = new Point(100, 20),
                    Size = new Size(150, 20),
                    Text = lastConfig?.Username ?? "" // 填充上次的用户名
                };

                var lblPassword = new Label { Text = "密码:", Location = new Point(20, 50), AutoSize = true };
                var txtPassword = new TextBox
                {
                    Location = new Point(100, 50),
                    Size = new Size(150, 20),
                    PasswordChar = '*',
                    Text = lastConfig?.Password ?? "" // 填充上次的密码
                };

                var btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(100, 90) };

                loginForm.Controls.AddRange(new Control[] { lblUsername, txtUsername, lblPassword, txtPassword, btnOK });
                loginForm.AcceptButton = btnOK;

                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;

                var config = new MySQLConfig
                {
                    ContainerId = containerId,
                    Username = txtUsername.Text,
                    Password = txtPassword.Text,
                    LastUsed = DateTime.Now
                };

                // 创建加载窗口
                using (var loadingForm = new LoadingForm(this, "正在连接数据库..."))
                {
                    try
                    {
                        loadingForm.Show();
                        Application.DoEvents();

                        using (var shellStream = sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024))
                        {
                            // 进入容器
                            shellStream.WriteLine($"docker exec -it {containerId} sh");
                            await Task.Delay(1000); // 等待shell准备就绪
                            var result = shellStream.Read();
                            Console.WriteLine($"Enter container result: {result}"); // 调试输出

                            // 分两步设置环境变量
                            shellStream.WriteLine($"export MYSQL_PWD='{config.Password}'");
                            await Task.Delay(500);
                            result = shellStream.Read();
                            Console.WriteLine($"Export result: {result}"); // 调试输出

                            shellStream.WriteLine("echo $MYSQL_PWD"); // 验证环境变量
                            await Task.Delay(500);
                            result = shellStream.Read();
                            Console.WriteLine($"Echo pwd result: {result}"); // 调试输出

                            // 执行MySQL命令
                            shellStream.WriteLine($"mysql -u{config.Username} -e 'SHOW DATABASES;'");
                            await Task.Delay(1000);
                            result = shellStream.Read();
                            Console.WriteLine($"MySQL result: {result}"); // 调试输出

                            if (result.Contains("Access denied"))
                            {
                                MessageBox.Show("MySQL登录失败，请检查用户名和密码", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            // 登录成功后保存配置
                            ConfigManager.SaveMySQLConfig(config);

                            // 解析数据库列表
                            var databases = result.Split('\n')
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Where(line => !line.StartsWith("mysql") &&  // 过滤掉命令行
                                              !line.StartsWith("-[") &&      // 过滤掉会话信息
                                              !line.StartsWith("#") &&       // 过滤掉提示符
                                              !line.Contains("SHOW DATABASES") && // 过滤掉命令
                                              !line.Contains("rows in set") &&    // 过滤掉结果统计
                                              !line.Contains("Database"))         // 过滤掉表头
                                .Select(db => db.Trim()
                                    .Replace("|", "")     // 删除竖线
                                    .Replace("-", "")     // 删除横线
                                    .Replace("+", "")     // 删除加号
                                    .Trim())             // 再次去除可能的空格
                                .Where(db => !string.IsNullOrWhiteSpace(db) &&
                                            !db.StartsWith("[") &&           // 过滤掉会话信息
                                            !db.EndsWith("#") &&            // 过滤掉提示符
                                            !db.Equals("information_schema", StringComparison.OrdinalIgnoreCase) &&  // 过滤系统数据库
                                            !db.Equals("mysql", StringComparison.OrdinalIgnoreCase) &&
                                            !db.Equals("performance_schema", StringComparison.OrdinalIgnoreCase) &&
                                            !db.Equals("sys", StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            Console.WriteLine("Available databases:"); // 调试输出
                            foreach (var db in databases)
                            {
                                Console.WriteLine($"- {db}");
                            }

                            // 获取完数据库列表后关闭加载窗口
                            loadingForm.Close();

                            // 显示数据库选择窗口
                            using (var dbSelectForm = new Form())
                            {
                                dbSelectForm.Text = "选择数据库";
                                dbSelectForm.Size = new Size(300, 400);
                                dbSelectForm.StartPosition = FormStartPosition.CenterParent;

                                var listBox = new ListBox
                                {
                                    Dock = DockStyle.Fill,
                                    SelectionMode = SelectionMode.One
                                };
                                listBox.Items.AddRange(databases.ToArray());

                                var btnSelect = new Button
                                {
                                    Text = "下载",
                                    Dock = DockStyle.Bottom
                                };

                                btnSelect.Click += async (s, e) =>
                                {
                                    if (listBox.SelectedItem == null) return;

                                    var selectedDB = listBox.SelectedItem.ToString().Trim();
                                    using (var saveFileDialog = new SaveFileDialog())
                                    {
                                        saveFileDialog.Filter = "SQL文件|*.sql";
                                        saveFileDialog.FileName = $"{selectedDB}_{DateTime.Now:yyyyMMdd}.sql";

                                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                                        {
                                            await DumpDatabase(containerId, config, selectedDB, saveFileDialog.FileName);
                                            dbSelectForm.Close();
                                        }
                                    }
                                };

                                dbSelectForm.Controls.AddRange(new Control[] { listBox, btnSelect });
                                dbSelectForm.ShowDialog();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        loadingForm.Close();
                        MessageBox.Show($"获取数据库列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task DumpDatabase(string containerId, MySQLConfig config, string database, string localPath)
        {
            using (var exportForm = new LoadingForm(this, "正在导出数据库..."))
            {
                try
                {
                    exportForm.Show();
                    Application.DoEvents();

                    using (var shellStream = sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024))
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd");
                        var dockerFilePath = $"/tmp/{database}_{timestamp}.sql";

                        // 进入容器
                        shellStream.WriteLine($"docker exec -it {containerId} sh");
                        await Task.Delay(1000);
                        var result = shellStream.Read();
                        Console.WriteLine($"Enter container result: {result}");

                        // 设置环境变量
                        shellStream.WriteLine($"export MYSQL_PWD='{config.Password}'");
                        await Task.Delay(500);
                        result = shellStream.Read();
                        Console.WriteLine($"Export result: {result}");

                        // 验证环境变量
                        shellStream.WriteLine("echo $MYSQL_PWD");
                        await Task.Delay(500);
                        result = shellStream.Read();
                        Console.WriteLine($"Echo pwd result: {result}");

                        // 执行mysqldump
                        shellStream.WriteLine($"mysqldump -u{config.Username} {database} > {dockerFilePath}");
                        await Task.Delay(2000);
                        result = shellStream.Read();
                        Console.WriteLine($"Dump result: {result}");

                        // 从容器复制到主机的临时目录
                        var hostTempPath = $"/tmp/{database}_{timestamp}.sql";
                        var copyCommand = sshClient.CreateCommand($"docker cp {containerId}:{dockerFilePath} {hostTempPath}");
                        copyCommand.Execute();

                        if (!string.IsNullOrEmpty(copyCommand.Error))
                        {
                            MessageBox.Show($"文件复制失败: {copyCommand.Error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // 使用SCP下载文件到本地
                        using (var downloadForm = new LoadingForm(this, "正在下载数据库...", true))
                        {
                            downloadForm.Show();
                            Application.DoEvents();

                            using (var scpClient = new ScpClient(sshClient.ConnectionInfo))
                            {
                                scpClient.Connect();

                                // 获取文件大小 - 使用 ls -l 命令
                                var sizeCommand = sshClient.CreateCommand($"ls -l {hostTempPath} | awk '{{print $5}}'");
                                var fileSizeStr = sizeCommand.Execute().Trim();
                                long totalSize;
                                if (!long.TryParse(fileSizeStr, out totalSize))
                                {
                                    // 如果无法获取文件大小，使用一个默认值
                                    totalSize = 100 * 1024 * 1024; // 假设100MB
                                }

                                scpClient.Downloading += (sender, e) =>
                                {
                                    downloadForm.UpdateProgress(e.Downloaded, totalSize);
                                };

                                using (var fileStream = new FileStream(localPath, FileMode.Create))
                                {
                                    await Task.Run(() => scpClient.Download(hostTempPath, fileStream));
                                }

                                scpClient.Disconnect();
                            }

                            downloadForm.Close();
                        }

                        // 清理远程临时文件（容器内和主机上的）
                        shellStream.WriteLine($"rm {dockerFilePath}");
                        await Task.Delay(500);

                        var cleanCommand = sshClient.CreateCommand($"rm {hostTempPath}");
                        cleanCommand.Execute();

                        exportForm.Close();
                        MessageBox.Show("数据库导出成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    exportForm.Close();
                    MessageBox.Show($"导出过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void ExecuteBuild(string containerId, string env)
        {
            try
            {
                // 创建日志窗口
                Form logForm = new Form
                {
                    Text = $"Build Logs - {env}",
                    Size = new System.Drawing.Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                RichTextBox logBox = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = Color.Black,
                    ForeColor = Color.LightGreen,
                    Font = new Font("Consolas", 10F),
                    Multiline = true,
                    ScrollBars = RichTextBoxScrollBars.Both
                };

                logForm.Controls.Add(logBox);
                logForm.Show();

                // 根据环境选择正确的构建命令
                string buildCommand = env == "staging" ? "build:staging" : "build:prod";
                string command = $"docker exec {containerId} yarn {buildCommand}";

                try
                {
                    var cmd = sshClient.CreateCommand(command);
                    
                    // 异步执行命令
                    _ = Task.Run(async () =>
                    {
                        try 
                        {
                            // 开始执行命令
                            var asyncResult = cmd.BeginExecute();
                            
                            // 创建读取器来读取输出流
                            using (var reader = new StreamReader(cmd.OutputStream))
                            using (var errorReader = new StreamReader(cmd.ExtendedOutputStream))
                            {
                                while (!asyncResult.IsCompleted || !reader.EndOfStream || !errorReader.EndOfStream)
                                {
                                    // 读取标准输出
                                    if (!reader.EndOfStream)
                                    {
                                        string line = await reader.ReadLineAsync();
                                        if (!string.IsNullOrEmpty(line))
                                        {
                                            await logBox.InvokeAsync(() =>
                                            {
                                                logBox.AppendText(line + Environment.NewLine);
                                                logBox.ScrollToCaret();
                                            });
                                        }
                                    }

                                    // 读取错误输出
                                    if (!errorReader.EndOfStream)
                                    {
                                        string errorLine = await errorReader.ReadLineAsync();
                                        if (!string.IsNullOrEmpty(errorLine))
                                        {
                                            await logBox.InvokeAsync(() =>
                                            {
                                                logBox.SelectionColor = Color.Red;
                                                logBox.AppendText(errorLine + Environment.NewLine);
                                                logBox.SelectionColor = logBox.ForeColor;
                                                logBox.ScrollToCaret();
                                            });
                                        }
                                    }

                                    await Task.Delay(100);
                                }
                            }

                            // 等待命令完成
                            cmd.EndExecute(asyncResult);

                            // 检查命令执行结果
                            if (cmd.ExitStatus != 0)
                            {
                                await logBox.InvokeAsync(() =>
                                {
                                    logBox.SelectionColor = Color.Red;
                                    logBox.AppendText($"\n\n构建失败，退出代码: {cmd.ExitStatus}\n");
                                    logBox.SelectionColor = logBox.ForeColor;
                                });
                            }
                            else
                            {
                                await logBox.InvokeAsync(() =>
                                {
                                    logBox.SelectionColor = Color.Green;
                                    logBox.AppendText("\n\n构建成功完成！\n");
                                    logBox.SelectionColor = logBox.ForeColor;
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            await logBox.InvokeAsync(() =>
                            {
                                logBox.SelectionColor = Color.Red;
                                logBox.AppendText($"\n\n执行命令时发生错误: {ex.Message}\n");
                                logBox.SelectionColor = logBox.ForeColor;
                            });
                        }
                    });

                    // 窗口关闭时取消命令
                    logForm.FormClosing += (s, e) =>
                    {
                        try
                        {
                            cmd.CancelAsync();
                        }
                        catch { }
                    };
                }
                catch (Exception ex)
                {
                    await logBox.InvokeAsync(() =>
                    {
                        logBox.AppendText($"\n\n执行命令失败: {ex.Message}");
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行打包时发生错误:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeployDev(string containerId)
        {
            await Deploy(containerId, "dev");
        }

        private async Task DeployProd(string containerId)
        {
            await Deploy(containerId, "prod");
        }

        private async Task Deploy(string containerId, string environment)
        {
            try
            {
                // 获取容器名称
                var command = sshClient.CreateCommand($"docker inspect --format='{{{{.Name}}}}' {containerId}");
                var containerName = command.Execute().Trim().TrimStart('/');

                // 加载部署配置
                var deployConfigs = ConfigManager.LoadDeployConfigs();
                var lastDeployConfig = deployConfigs.FirstOrDefault(c => 
                    c.ContainerId == containerId && 
                    c.Environment == environment);

                // 创建部署配置窗口
                using (var deployForm = new Form())
                {
                    deployForm.Text = environment == "dev" 
                        ? $"开发环境部署配置 - {containerName}"
                        : $"生产环境部署配置 - {containerName}";
                    deployForm.Size = new Size(500, 320);
                    deployForm.StartPosition = FormStartPosition.CenterParent;
                    deployForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    deployForm.MaximizeBox = false;
                    deployForm.MinimizeBox = false;

                    // SSH服务器选择
                    var lblServer = new Label { Text = "目标服务器:", Location = new Point(20, 20), AutoSize = true };
                    var cboServer = new ComboBox 
                    { 
                        Location = new Point(20, 45), 
                        Size = new Size(440, 23),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    // 本地目录选择
                    var lblLocalPath = new Label { Text = "本地目录:", Location = new Point(20, 80), AutoSize = true };
                    var txtLocalPath = new TextBox 
                    { 
                        Location = new Point(20, 105), 
                        Size = new Size(350, 23),
                        Text = lastDeployConfig?.LocalPath ?? "" // 填充上次的本地目录
                    };

                    // SSH目录输入
                    var lblRemotePath = new Label { Text = "远程目录:", Location = new Point(20, 140), AutoSize = true };
                    var txtRemotePath = new TextBox 
                    { 
                        Location = new Point(20, 165), 
                        Size = new Size(440, 23),
                        Text = lastDeployConfig?.RemotePath ?? "/var/www/html/dist" // 填充上次的远程目录
                    };

                    // 加载SSH配置
                    var sshConfigs = ConfigManager.LoadSSHConfigs();
                    var allDeployConfigs = ConfigManager.LoadDeployConfigs();

                    // 创建服务器选择改变事件处理器
                    cboServer.SelectedIndexChanged += (s, e) =>
                    {
                        if (cboServer.SelectedItem != null)
                        {
                            var selectedServer = ((SSHConfigItem)cboServer.SelectedItem).Config;
                            // 查找匹配的部署配置
                            var matchingConfig = allDeployConfigs.FirstOrDefault(c => 
                                c.ContainerId == containerId && 
                                c.Environment == environment &&
                                c.ServerHost == selectedServer.Host &&
                                c.ServerPort == selectedServer.Port);

                            if (matchingConfig != null)
                            {
                                txtLocalPath.Text = matchingConfig.LocalPath;
                                txtRemotePath.Text = matchingConfig.RemotePath;
                            }
                            else
                            {
                                // 如果没有找到匹配的配置，设置默认值
                                var command = sshClient.CreateCommand($"docker inspect --format='{{{{.Config.WorkingDir}}}}' {containerId}");
                                var workDir = command.Execute().Trim();
                                
                                // 设置本地目录
                                txtLocalPath.Text = $"{workDir}/dist";
                                
                                // 根据环境设置不同的远程目录
                                if (environment == "dev")
                                {
                                    txtRemotePath.Text = "/var/www/html/dev";
                                }
                                else if (environment == "prod")
                                {
                                    txtRemotePath.Text = "/var/www/html/dist";
                                }
                            }
                        }
                    };

                    foreach (var config in sshConfigs)
                    {
                        var item = new SSHConfigItem(config);
                        cboServer.Items.Add(item);
                        
                        // 如果有上次部署的配置，选中对应的服务器
                        if (lastDeployConfig != null && 
                            config.Host == lastDeployConfig.ServerHost && 
                            config.Port == lastDeployConfig.ServerPort)
                        {
                            cboServer.SelectedItem = item;
                        }
                    }

                    // 如果没有找到上次的服务器，选择第一个
                    if (cboServer.SelectedIndex == -1 && cboServer.Items.Count > 0)
                    {
                        cboServer.SelectedIndex = 0;
                    }

                    var btnBrowse = new Button { Text = "浏览", Location = new Point(380, 104), Size = new Size(80, 25) };
                    
                    btnBrowse.Click += (s, e) =>
                    {
                        using (var folderDialog = new FolderBrowserDialog())
                        {
                            if (!string.IsNullOrEmpty(txtLocalPath.Text))
                            {
                                folderDialog.SelectedPath = txtLocalPath.Text;
                            }
                            if (folderDialog.ShowDialog() == DialogResult.OK)
                            {
                                txtLocalPath.Text = folderDialog.SelectedPath;
                            }
                        }
                    };

                    // 上传按钮
                    var btnUpload = new Button
                    {
                        Text = "开始上传",
                        Location = new Point(200, 220),
                        Size = new Size(100, 30)
                    };

                    btnUpload.Click += async (s, e) =>
                    {
                        if (cboServer.SelectedItem == null)
                        {
                            MessageBox.Show("请选择目标服务器", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(txtLocalPath.Text) || string.IsNullOrWhiteSpace(txtRemotePath.Text))
                        {
                            MessageBox.Show("请填写本地目录和远程目录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var selectedConfig = ((SSHConfigItem)cboServer.SelectedItem).Config;
                        
                        // 保存当前配置
                        var deployConfig = new DeployConfig
                        {
                            ContainerId = containerId,
                            ContainerName = containerName,
                            Environment = environment,
                            ServerName = selectedConfig.Name,
                            ServerHost = selectedConfig.Host,
                            ServerPort = selectedConfig.Port,
                            LocalPath = txtLocalPath.Text,
                            RemotePath = txtRemotePath.Text,
                            LastUsed = DateTime.Now
                        };
                        ConfigManager.SaveDeployConfig(deployConfig);

                        deployForm.Close();
                        await UploadFiles(containerId, txtLocalPath.Text, txtRemotePath.Text, selectedConfig);
                    };

                    deployForm.Controls.AddRange(new Control[] {
                        lblServer, cboServer,
                        lblLocalPath, txtLocalPath, btnBrowse,
                        lblRemotePath, txtRemotePath,
                        btnUpload
                    });

                    deployForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"部署过程中发生错误:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 用于ComboBox显示的SSH配置项类
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

        private async Task UploadFiles(string containerId, string localPath, string remotePath, SSHConfig targetServer)
        {
            SshClient targetSshClient = null;
            try
            {
                // 连接目标服务器
                targetSshClient = new SshClient(targetServer.Host, targetServer.Port, targetServer.Username, targetServer.Password);
                targetSshClient.Connect();

                using (var uploadForm = new LoadingForm(this, "正在上传文件...", true))
                {
                    uploadForm.Show();
                    Application.DoEvents();

                    try
                    {
                        // 压缩本地文件
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);
                        var zipPath = Path.Combine(tempDir, "dist.zip");
                        
                        ZipFile.CreateFromDirectory(localPath, zipPath);

                        // 获取文件大小
                        var fileInfo = new FileInfo(zipPath);
                        var totalSize = fileInfo.Length;

                        // 上传到远程主机的临时目录
                        using (var scpClient = new ScpClient(targetSshClient.ConnectionInfo))
                        {
                            scpClient.Connect();

                            // 设置进度回调
                            scpClient.Uploading += (sender, e) =>
                            {
                                uploadForm.UpdateProgress(e.Uploaded, totalSize);
                            };

                            // 上传文件
                            using (var fs = new FileStream(zipPath, FileMode.Open))
                            {
                                await Task.Run(() => scpClient.Upload(fs, "/tmp/dist.zip"));
                            }

                            scpClient.Disconnect();
                        }

                        // 在目标服务器上解压文件
                        var cmd = targetSshClient.CreateCommand($"cd {remotePath} && unzip -o /tmp/dist.zip && rm -f /tmp/dist.zip");
                        
                        var result = cmd.Execute();
                        var error = cmd.Error;

                        if (!string.IsNullOrEmpty(error))
                        {
                            throw new Exception($"在目标服务器上解压文件失败: {error}");
                        }

                        uploadForm.Close();
                        MessageBox.Show("部署成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 清理临时目录
                        try
                        {
                            if (Directory.Exists(tempDir))
                            {
                                Directory.Delete(tempDir, true);
                            }
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"处理文件失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传文件失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (targetSshClient != null)
                {
                    if (targetSshClient.IsConnected)
                    {
                        targetSshClient.Disconnect();
                    }
                    targetSshClient.Dispose();
                }
            }
        }

        private void BtnBatchOperation_Click(object sender, EventArgs e)
        {
            var batchForm = new BatchOperationForm(
                listViewContainers, 
                containerContextMenu,
                ExecuteDockerCommand,
                ShowContainerLogs,
                ExecuteBuild,
                Deploy,
                sshClient
            );
            batchForm.ShowDialog();
            LoadContainers(); // 刷新容器列表
        }

        private class BatchOperationForm : Form
        {
            private TreeView treeView;
            private Button btnExecute;
            private Button btnCancel;
            private ListView containerList;
            private ContextMenuStrip contextMenu;
            private Action<string, string> executeDockerCommand;
            private Action<string> showContainerLogs;
            private Action<string, string> executeBuild;
            private Func<string, string, Task> deploy;
            private Renci.SshNet.SshClient sshClient;

            public BatchOperationForm(
                ListView containers, 
                ContextMenuStrip menu,
                Action<string, string> executeDockerCommand,
                Action<string> showContainerLogs,
                Action<string, string> executeBuild,
                Func<string, string, Task> deploy,
                Renci.SshNet.SshClient sshClient)
            {
                containerList = containers;
                contextMenu = menu;
                this.executeDockerCommand = executeDockerCommand;
                this.showContainerLogs = showContainerLogs;
                this.executeBuild = executeBuild;
                this.deploy = deploy;
                this.sshClient = sshClient;
                InitializeUI();
                LoadContainers();
            }

            private void InitializeUI()
            {
                this.Text = "批量操作";
                this.Size = new Size(500, 600);
                this.StartPosition = FormStartPosition.CenterParent;

                treeView = new TreeView
                {
                    Location = new Point(10, 10),
                    Size = new Size(460, 500),
                    CheckBoxes = true,
                    ShowLines = true,
                    ShowPlusMinus = true,
                    ShowRootLines = true,
                    Font = new Font("Microsoft YaHei UI", 10F),
                    FullRowSelect = true,
                    HotTracking = true,
                    LabelEdit = false
                };

                // 添加 TreeView 的节点选中事件
                treeView.AfterCheck += TreeView_AfterCheck;
                treeView.NodeMouseClick += TreeView_NodeMouseClick;

                btnExecute = new Button
                {
                    Text = "执行",
                    Location = new Point(290, 520),
                    Size = new Size(80, 30)
                };
                btnExecute.Click += BtnExecute_Click;

                btnCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(390, 520),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.Cancel
                };

                this.Controls.AddRange(new Control[] { treeView, btnExecute, btnCancel });
                this.CancelButton = btnCancel;
            }

            private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
            {
                // 如果点击的不是复选框区域，则切换节点的选中状态
                if (e.Location.X > 20) // 20是复选框的大致宽度
                {
                    e.Node.Checked = !e.Node.Checked;
                }
            }

            private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
            {
                // 防止事件循环
                if (e.Action == TreeViewAction.Unknown) return;

                // 如果是容器节点被选中/取消选中
                if (e.Node.Parent == null)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        if (child.Checked != e.Node.Checked)
                        {
                            child.Checked = e.Node.Checked;
                        }
                    }
                }
                // 如果是操作节点被选中/取消选中
                else
                {
                    bool allChecked = true;
                    bool anyChecked = false;
                    foreach (TreeNode child in e.Node.Parent.Nodes)
                    {
                        if (child.Checked)
                        {
                            anyChecked = true;
                        }
                        else
                        {
                            allChecked = false;
                        }
                    }
                    
                    if (e.Node.Parent.Checked != allChecked)
                    {
                        e.Node.Parent.Checked = allChecked;
                    }
                }
            }

            private void LoadContainers()
            {
                treeView.Nodes.Clear();

                foreach (ListViewItem item in containerList.Items)
                {
                    if (item.SubItems[4].Text.Contains("Up"))  // 只显示运行中的容器
                    {
                        var containerNode = new TreeNode
                        {
                            Text = item.SubItems[1].Text,  // 只显示镜像名称
                            Tag = new ContainerInfo 
                            { 
                                Id = item.Text,
                                Name = item.SubItems[2].Text,
                                Image = item.SubItems[1].Text
                            }
                        };

                        // 添加操作节点
                        var operations = new[]
                        {
                            new { Text = "停止容器", Tag = "stop" },
                            new { Text = "重启容器", Tag = "restart" },
                            new { Text = "查看日志", Tag = "logs" },
                            new { Text = "打包Dev环境", Tag = "build_dev" },
                            new { Text = "打包Prod环境", Tag = "build_prod" },
                            new { Text = "部署到Dev环境", Tag = "deploy_dev" },
                            new { Text = "部署到Prod环境", Tag = "deploy_prod" },
                            new { Text = "开发环境打包及部署", Tag = "build_deploy_dev" },
                            new { Text = "生产环境打包及部署", Tag = "build_deploy_prod" }
                        };

                        foreach (var op in operations)
                        {
                            containerNode.Nodes.Add(new TreeNode
                            {
                                Text = op.Text,
                                Tag = op.Tag
                            });
                        }

                        treeView.Nodes.Add(containerNode);
                    }
                }

                if (treeView.Nodes.Count > 0)
                {
                    treeView.ExpandAll();
                }
            }

            private class ContainerInfo
            {
                public string Id { get; set; }
                public string Name { get; set; }
                public string Image { get; set; }
            }

            private async void BtnExecute_Click(object sender, EventArgs e)
            {
                // 按容器分组收集操作，保持TreeView中的顺序
                var operationsByContainer = new List<(ContainerInfo Container, List<string> Operations)>();

                foreach (TreeNode containerNode in treeView.Nodes)
                {
                    var containerInfo = (ContainerInfo)containerNode.Tag;
                    var operations = new List<string>();
                    
                    foreach (TreeNode operationNode in containerNode.Nodes)
                    {
                        if (operationNode.Checked)
                        {
                            operations.Add(operationNode.Tag.ToString());
                        }
                    }

                    if (operations.Count > 0)
                    {
                        operationsByContainer.Add((containerInfo, operations));
                    }
                }

                if (operationsByContainer.Count == 0)
                {
                    MessageBox.Show("请选择要执行的操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 构建确认消息
                var confirmMessage = new StringBuilder("您确定要按以下顺序执行操作吗？\n\n");
                foreach (var (container, operations) in operationsByContainer)
                {
                    confirmMessage.AppendLine($"容器：{container.Image}");
                    confirmMessage.AppendLine("操作顺序：");
                    for (int i = 0; i < operations.Count; i++)
                    {
                        confirmMessage.AppendLine($"  {i + 1}. {GetOperationText(operations[i])}");
                    }
                    confirmMessage.AppendLine();
                }

                if (MessageBox.Show(
                    confirmMessage.ToString(),
                    "确认执行顺序",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question) == DialogResult.OK)
                {
                    this.Enabled = false;
                    try 
                    {
                        // 按顺序执行每个容器的操作
                        foreach (var (container, operations) in operationsByContainer)
                        {
                            // 显示当前正在处理的容器
                            this.Text = $"批量操作 - 正在处理: {container.Image}";
                            
                            foreach (var operation in operations)
                            {
                                // 显示当前正在执行的操作
                                this.Text = $"批量操作 - {container.Image} - {GetOperationText(operation)}";

                                // 等待所有上传窗口关闭后再继续下一个操作
                                while (Application.OpenForms.OfType<Form>().Any(f => 
                                    f.Text.Contains("正在上传文件") || 
                                    f.Text.Contains("开发环境部署配置") ||
                                    f.Text.Contains("生产环境部署配置")))
                                {
                                    await Task.Delay(500);
                                    Application.DoEvents();
                                }
                                
                                if (operation == "build_deploy_dev" || operation == "build_deploy_prod")
                                {
                                    var env = operation == "build_deploy_dev" ? "staging" : "prod";
                                    var deployEnv = operation == "build_deploy_dev" ? "dev" : "prod";
                                    
                                    // 创建一个TaskCompletionSource来等待整个操作完成
                                    var operationCompleted = new TaskCompletionSource<bool>();
                                    
                                    // 先执行构建
                                    var buildCommand = $"docker exec {container.Id} yarn build:{env}";
                                    var cmd = sshClient.CreateCommand(buildCommand);
                                    
                                    // 创建日志窗口
                                    Form logForm = new Form
                                    {
                                        Text = $"Build Logs - {container.Image} - {env}",
                                        Size = new Size(800, 600),
                                        StartPosition = FormStartPosition.CenterParent
                                    };

                                    RichTextBox logBox = new RichTextBox
                                    {
                                        Dock = DockStyle.Fill,
                                        ReadOnly = true,
                                        BackColor = Color.Black,
                                        ForeColor = Color.LightGreen,
                                        Font = new Font("Consolas", 10F),
                                        Multiline = true,
                                        ScrollBars = RichTextBoxScrollBars.Both
                                    };

                                    logForm.Controls.Add(logBox);
                                    logForm.Show();

                                    try 
                                    {
                                        var asyncResult = cmd.BeginExecute();
                                        
                                        using (var reader = new StreamReader(cmd.OutputStream))
                                        using (var errorReader = new StreamReader(cmd.ExtendedOutputStream))
                                        {
                                            while (!asyncResult.IsCompleted || !reader.EndOfStream || !errorReader.EndOfStream)
                                            {
                                                if (!reader.EndOfStream)
                                                {
                                                    string line = await reader.ReadLineAsync();
                                                    if (!string.IsNullOrEmpty(line))
                                                    {
                                                        logBox.Invoke((MethodInvoker)delegate
                                                        {
                                                            logBox.AppendText(line + Environment.NewLine);
                                                            logBox.ScrollToCaret();
                                                        });
                                                    }
                                                }

                                                if (!errorReader.EndOfStream)
                                                {
                                                    string errorLine = await errorReader.ReadLineAsync();
                                                    if (!string.IsNullOrEmpty(errorLine))
                                                    {
                                                        logBox.Invoke((MethodInvoker)delegate
                                                        {
                                                            logBox.SelectionColor = Color.Red;
                                                            logBox.AppendText(errorLine + Environment.NewLine);
                                                            logBox.SelectionColor = logBox.ForeColor;
                                                            logBox.ScrollToCaret();
                                                        });
                                                    }
                                                }

                                                await Task.Delay(100);
                                            }
                                        }

                                        cmd.EndExecute(asyncResult);

                                        if (cmd.ExitStatus == 0)
                                        {
                                            logBox.Invoke((MethodInvoker)delegate
                                            {
                                                logBox.SelectionColor = Color.Green;
                                                logBox.AppendText("\n\n构建成功完成！\n");
                                                logBox.SelectionColor = logBox.ForeColor;
                                            });
                                            
                                            // 等待一会儿显示成功消息
                                            await Task.Delay(1000);
                                            logForm.Invoke((MethodInvoker)delegate
                                            {
                                                logForm.Close();
                                            });
                                            
                                            // 构建成功后执行部署，等待部署完成
                                            var uploadCompleted = new TaskCompletionSource<bool>();
                                            
                                            // 创建一个计时器来检查上传窗口
                                            var uploadCheckTimer = new System.Windows.Forms.Timer();
                                            uploadCheckTimer.Interval = 500;
                                            
                                            // 标记是否已经开始部署
                                            bool deployStarted = false;
                                            
                                            uploadCheckTimer.Tick += async (s, ev) => {
                                                var uploadForm = Application.OpenForms.OfType<Form>()
                                                    .FirstOrDefault(f => f.Text.Contains("正在上传文件"));
                                                    
                                                if (!deployStarted)
                                                {
                                                    deployStarted = true;
                                                    try 
                                                    {
                                                        await deploy(container.Id, deployEnv);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        MessageBox.Show($"部署过程发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                        uploadCompleted.TrySetResult(false);
                                                    }
                                                }
                                                
                                                if (uploadForm == null)
                                                {
                                                    // 如果没有上传窗口，并且已经过了2秒（确保窗口真的关闭了）
                                                    await Task.Delay(2000);
                                                    if (!Application.OpenForms.OfType<Form>().Any(f => 
                                                        f.Text.Contains("正在上传文件") || 
                                                        f.Text.Contains("开发环境部署配置") ||
                                                        f.Text.Contains("生产环境部署配置")))
                                                    {
                                                        uploadCheckTimer.Stop();
                                                        uploadCompleted.TrySetResult(true);
                                                    }
                                                }
                                            };
                                            
                                            // 启动计时器
                                            uploadCheckTimer.Start();
                                            
                                            // 等待上传完成
                                            await uploadCompleted.Task;
                                            
                                            // 标记操作完成
                                            operationCompleted.SetResult(true);
                                        }
                                        else
                                        {
                                            logBox.Invoke((MethodInvoker)delegate
                                            {
                                                logBox.SelectionColor = Color.Red;
                                                logBox.AppendText($"\n\n构建失败，退出代码: {cmd.ExitStatus}\n");
                                                logBox.SelectionColor = logBox.ForeColor;
                                            });
                                            operationCompleted.SetResult(false);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logBox.Invoke((MethodInvoker)delegate
                                        {
                                            logBox.SelectionColor = Color.Red;
                                            logBox.AppendText($"\n\n执行命令时发生错误: {ex.Message}\n");
                                            logBox.SelectionColor = logBox.ForeColor;
                                        });
                                        operationCompleted.SetResult(false);
                                    }

                                    // 等待操作完成
                                    await operationCompleted.Task;
                                    
                                    // 如果操作失败，询问是否继续执行其他操作
                                    if (!operationCompleted.Task.Result)
                                    {
                                        if (MessageBox.Show(
                                            $"{container.Image} 的操作失败。是否继续执行其他操作？",
                                            "操作失败",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question) == DialogResult.No)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // 其他操作保持不变
                                    switch (operation)
                                    {
                                        case "stop":
                                            executeDockerCommand($"docker stop {container.Id}", "停止容器");
                                            break;
                                        case "restart":
                                            executeDockerCommand($"docker restart {container.Id}", "重启容器");
                                            break;
                                        case "logs":
                                            showContainerLogs(container.Id);
                                            break;
                                        case "build_dev":
                                            executeBuild(container.Id, "staging");
                                            break;
                                        case "build_prod":
                                            executeBuild(container.Id, "prod");
                                            break;
                                        case "deploy_dev":
                                            await deploy(container.Id, "dev");
                                            // 等待部署完成，包括上传过程
                                            while (Application.OpenForms.OfType<Form>().Any(f => 
                                                f.Text.Contains("正在上传文件") || 
                                                f.Text.Contains("开发环境部署配置")))
                                            {
                                                await Task.Delay(500);
                                                Application.DoEvents();
                                            }
                                            break;
                                        case "deploy_prod":
                                            await deploy(container.Id, "prod");
                                            // 等待部署完成，包括上传过程
                                            while (Application.OpenForms.OfType<Form>().Any(f => 
                                                f.Text.Contains("正在上传文件") || 
                                                f.Text.Contains("生产环境部署配置")))
                                            {
                                                await Task.Delay(500);
                                                Application.DoEvents();
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        this.DialogResult = DialogResult.OK;
                    }
                    finally 
                    {
                        this.Enabled = true;
                        this.Text = "批量操作";
                    }
                }
            }

            private ToolStripMenuItem FindMenuItemByTag(ToolStripItemCollection items, string tag)
            {
                foreach (ToolStripItem item in items)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        if (GetOperationTag(menuItem.Text) == tag)
                        {
                            return menuItem;
                        }
                        
                        if (menuItem.DropDownItems.Count > 0)
                        {
                            var found = FindMenuItemByTag(menuItem.DropDownItems, tag);
                            if (found != null)
                            {
                                return found;
                            }
                        }
                    }
                }
                return null;
            }

            private string GetOperationText(string tag)
            {
                var operationTexts = new Dictionary<string, string>
                {
                    { "stop", "停止容器" },
                    { "restart", "重启容器" },
                    { "logs", "查看日志" },
                    { "build_dev", "打包Dev环境" },
                    { "build_prod", "打包Prod环境" },
                    { "deploy_dev", "部署到Dev环境" },
                    { "deploy_prod", "部署到Prod环境" },
                    { "build_deploy_dev", "开发环境打包及部署" },
                    { "build_deploy_prod", "生产环境打包及部署" }
                };

                return operationTexts.TryGetValue(tag, out var text) ? text : tag;
            }

            private string GetOperationTag(string text)
            {
                var operationTags = new Dictionary<string, string>
                {
                    { "停止容器", "stop" },
                    { "重启容器", "restart" },
                    { "查看日志", "logs" },
                    { "打包Dev环境", "build_dev" },
                    { "打包Prod环境", "build_prod" },
                    { "部署到Dev环境", "deploy_dev" },
                    { "部署到Prod环境", "deploy_prod" },
                    { "开发环境打包及部署", "build_deploy_dev" },
                    { "生产环境打包及部署", "build_deploy_prod" }
                };

                return operationTags.TryGetValue(text, out var tag) ? tag : text;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // 不在这里处理SSH客户端的断开和释放，因为它是由父窗口管理的
        }
    }
} 