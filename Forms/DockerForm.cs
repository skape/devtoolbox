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
                logsItem,
                inspectItem,
                downloadDBItem
            });

            // 在显示菜单前检查容器状态并启用/禁用相应选项
            containerContextMenu.Opening += (s, e) =>
            {
                string status = GetSelectedContainerStatus();
                startItem.Enabled = status?.Contains("Exited") ?? false;
                stopItem.Enabled = status?.Contains("Up") ?? false;
                restartItem.Enabled = status?.Contains("Up") ?? false;
                string image = GetSelectedContainerImage()?.ToLower() ?? "";
                downloadDBItem.Visible = image.Contains("mysql");
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

        private void InitializeComponent()
        {

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