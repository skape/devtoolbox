using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using DevToolbox.Models;
using DevToolbox.Utils;
using Renci.SshNet;
using System.Text;
using System.Threading;

namespace DevToolbox.Forms
{
    public class JavaDeployForm : Form
    {
        private TextBox txtProjectPath;
        private ComboBox cboServers;
        private TextBox txtRemotePath;
        private TextBox txtDeployScript;
        private Button btnBrowseProject;
        private Button btnDeploy;
        private Button btnCancel;

        public JavaDeployForm()
        {
            InitializeComponent();
            LoadLastConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Java项目部署";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Java项目路径
            var lblProjectPath = new Label
            {
                Text = "项目路径:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            txtProjectPath = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(350, 23)
            };

            btnBrowseProject = new Button
            {
                Text = "浏览",
                Location = new Point(480, 20),
                Size = new Size(80, 23)
            };
            btnBrowseProject.Click += BtnBrowseProject_Click;

            // 服务器选择
            var lblServer = new Label
            {
                Text = "目标服务器:",
                Location = new Point(20, 60),
                AutoSize = true
            };

            cboServers = new ComboBox
            {
                Location = new Point(120, 60),
                Size = new Size(440, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 远程目录
            var lblRemotePath = new Label
            {
                Text = "远程目录:",
                Location = new Point(20, 100),
                AutoSize = true
            };

            txtRemotePath = new TextBox
            {
                Location = new Point(120, 100),
                Size = new Size(440, 23)
            };

            // 部署脚本
            var lblDeployScript = new Label
            {
                Text = "部署脚本:",
                Location = new Point(20, 140),
                AutoSize = true
            };

            txtDeployScript = new TextBox
            {
                Location = new Point(120, 140),
                Size = new Size(440, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Both
            };

            // 按钮
            btnDeploy = new Button
            {
                Text = "开始部署",
                Location = new Point(200, 300),
                Size = new Size(100, 30)
            };
            btnDeploy.Click += BtnDeploy_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(320, 300),
                Size = new Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            // 加载SSH服务器列表
            LoadServerList();

            // 添加控件
            this.Controls.AddRange(new Control[]
            {
                lblProjectPath, txtProjectPath, btnBrowseProject,
                lblServer, cboServers,
                lblRemotePath, txtRemotePath,
                lblDeployScript, txtDeployScript,
                btnDeploy, btnCancel
            });
        }

        private void LoadServerList()
        {
            var configs = ConfigManager.LoadSSHConfigs();
            foreach (var config in configs)
            {
                cboServers.Items.Add(new SSHConfigItem(config));
            }

            if (cboServers.Items.Count > 0)
            {
                cboServers.SelectedIndex = 0;
            }
        }

        private void LoadLastConfig()
        {
            try
            {
                var configs = ConfigManager.LoadJavaDeployConfigs();
                if (configs.Count > 0)
                {
                    var lastConfig = configs.OrderByDescending(c => c.LastUsed).First();
                    txtProjectPath.Text = lastConfig.ProjectPath;
                    txtRemotePath.Text = lastConfig.RemotePath;
                    txtDeployScript.Text = lastConfig.DeployScript;

                    // 选择对应的服务器
                    for (int i = 0; i < cboServers.Items.Count; i++)
                    {
                        var item = (SSHConfigItem)cboServers.Items[i];
                        if (item.Config.Host == lastConfig.ServerHost &&
                            item.Config.Port == lastConfig.ServerPort)
                        {
                            cboServers.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载上次配置失败: {ex.Message}");
            }
        }

        private void BtnBrowseProject_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(txtProjectPath.Text))
                {
                    folderDialog.SelectedPath = txtProjectPath.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtProjectPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void BtnDeploy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProjectPath.Text))
            {
                MessageBox.Show("请选择Java项目路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(Path.Combine(txtProjectPath.Text, "pom.xml")))
            {
                MessageBox.Show("所选目录不是有效的Maven项目，请确保目录中包含pom.xml文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (cboServers.SelectedItem == null)
            {
                MessageBox.Show("请选择目标服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRemotePath.Text))
            {
                MessageBox.Show("请输入远程目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDeployScript.Text))
            {
                MessageBox.Show("请输入部署脚本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedServer = ((SSHConfigItem)cboServers.SelectedItem).Config;

            // 保存配置
            var config = new JavaDeployConfig
            {
                ProjectPath = txtProjectPath.Text,
                ServerName = selectedServer.Name,
                ServerHost = selectedServer.Host,
                ServerPort = selectedServer.Port,
                RemotePath = txtRemotePath.Text,
                DeployScript = txtDeployScript.Text,
                LastUsed = DateTime.Now
            };
            ConfigManager.SaveJavaDeployConfig(config);

            try
            {
                // 创建日志窗口
                Form logForm = new Form
                {
                    Text = "Maven Build Logs",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                var cancellationTokenSource = new CancellationTokenSource();

                logForm.FormClosing += (s, args) =>
                {
                    if (MessageBox.Show("确定要取消部署吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        args.Cancel = true;
                    }
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

                // 在后台线程中执行部署
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 执行Maven打包
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/C cd /d \"{txtProjectPath.Text}\" && mvn -T 4C package -DskipTests",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        var processExited = new TaskCompletionSource<bool>();

                        process.OutputDataReceived += (s, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                SafeAppendText(logBox, args.Data + Environment.NewLine);
                            }
                        };

                        process.ErrorDataReceived += (s, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                SafeAppendText(logBox, args.Data + Environment.NewLine, Color.Red);
                            }
                        };

                        process.Exited += (s, e) => processExited.SetResult(true);
                        process.EnableRaisingEvents = true;

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        using var registration = cancellationTokenSource.Token.Register(() =>
                        {
                            try { process.Kill(); } catch { }
                        });

                        await Task.WhenAny(processExited.Task, Task.Delay(-1, cancellationTokenSource.Token));

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            SafeAppendText(logBox, "\n部署已取消\n", Color.Yellow);
                            return;
                        }

                        if (process.ExitCode != 0)
                        {
                            SafeAppendText(logBox, "\nMaven打包失败！\n", Color.Red);
                            return;
                        }

                        SafeAppendText(logBox, "\nMaven打包成功！准备上传文件...\n", Color.Green);

                        // 查找生成的JAR文件
                        var targetDir = Path.Combine(txtProjectPath.Text, "target");
                        var jarFiles = Directory.GetFiles(targetDir, "*.jar")
                            .Where(f => !f.EndsWith("-sources.jar") && !f.EndsWith("-javadoc.jar"))
                            .ToList();

                        if (jarFiles.Count == 0)
                        {
                            SafeAppendText(logBox, "\n未找到生成的JAR文件\n", Color.Red);
                            return;
                        }

                        var jarFile = jarFiles[0];

                        SafeAppendText(logBox, "\n正在连接服务器...\n");

                        using (var sshClient = new SshClient(selectedServer.Host, selectedServer.Port, selectedServer.Username, selectedServer.Password))
                        {
                            sshClient.Connect();

                            SafeAppendText(logBox, $"\n创建远程目录: {txtRemotePath.Text}\n");

                            var mkdirCmd = sshClient.CreateCommand($"mkdir -p {txtRemotePath.Text}");
                            mkdirCmd.Execute();

                            SafeAppendText(logBox, "\n正在上传JAR文件...\n");

                            using (var scpClient = new ScpClient(sshClient.ConnectionInfo))
                            {
                                scpClient.Connect();

                                var fileInfo = new FileInfo(jarFile);
                                var fileName = Path.GetFileName(jarFile);
                                var remotePath = txtRemotePath.Text.TrimEnd('/') + "/" + fileName;

                                SafeAppendText(logBox, $"\n目标路径: {remotePath}\n");

                                var lastBytes = 0L;
                                var lastUpdateTime = DateTime.Now;
                                var startTime = DateTime.Now;

                                LoadingForm uploadForm = null;
                                logForm.Invoke(new Action(() => 
                                {
                                    uploadForm = new LoadingForm(logForm, "正在上传文件...", true);
                                    uploadForm.Show();
                                    Application.DoEvents();
                                }));

                                try 
                                {
                                    scpClient.Uploading += (sender, e) =>
                                    {
                                        var now = DateTime.Now;
                                        var progress = (double)e.Uploaded / fileInfo.Length * 100;
                                        var instantSpeed = (e.Uploaded - lastBytes) / Math.Max(1, (now - lastUpdateTime).TotalSeconds);
                                        var remainingBytes = fileInfo.Length - e.Uploaded;
                                        var estimatedSecondsRemaining = remainingBytes / Math.Max(1, instantSpeed);

                                        logForm.Invoke(new Action(() =>
                                        {
                                            if (uploadForm != null && !uploadForm.IsDisposed)
                                            {
                                                uploadForm.UpdateProgress(
                                                    e.Uploaded,
                                                    fileInfo.Length,
                                                    $"进度: {progress:F1}% ({FormatFileSize(e.Uploaded)}/{FormatFileSize(fileInfo.Length)})\n" +
                                                    $"速度: {FormatFileSize((long)instantSpeed)}/s\n" +
                                                    $"预计剩余时间: {FormatTimeSpan(TimeSpan.FromSeconds(estimatedSecondsRemaining))}"
                                                );
                                            }
                                        }));

                                        lastBytes = e.Uploaded;
                                        lastUpdateTime = now;
                                    };

                                    // 上传文件
                                    using (var fs = new FileStream(jarFile, FileMode.Open))
                                    {
                                        await Task.Run(() => scpClient.Upload(fs, remotePath));
                                    }
                                }
                                finally
                                {
                                    logForm.Invoke(new Action(() =>
                                    {
                                        if (uploadForm != null && !uploadForm.IsDisposed)
                                        {
                                            uploadForm.Close();
                                            uploadForm.Dispose();
                                        }
                                    }));
                                }

                                if (cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    SafeAppendText(logBox, "\n上传已取消\n", Color.Yellow);
                                    return;
                                }

                                SafeAppendText(logBox, $"\n文件上传完成: {remotePath}\n");

                                // 执行部署脚本
                                SafeAppendText(logBox, "\n正在执行部署脚本...\n");
                                SafeAppendText(logBox, $"脚本路径: {txtDeployScript.Text}\n");

                                // 确保脚本有执行权限
                                var chmodCmd = sshClient.CreateCommand($"chmod +x {txtDeployScript.Text}");
                                await Task.Run(() => chmodCmd.Execute());

                                // 执行脚本文件并实时显示输出
                                var deployCmd = sshClient.CreateCommand($"bash {txtDeployScript.Text}");
                                deployCmd.CommandTimeout = TimeSpan.FromHours(1);

                                // 设置输出流
                                using var outputReader = new StreamReader(deployCmd.OutputStream);
                                using var errorReader = new StreamReader(deployCmd.ExtendedOutputStream);

                                // 在后台线程中执行命令并处理输出
                                var executeTask = Task.Run(async () =>
                                {
                                    IAsyncResult asyncResult = null;
                                    try
                                    {
                                        // 开始执行命令
                                        asyncResult = deployCmd.BeginExecute();
                                        
                                        // 持续读取输出直到命令完成
                                        while (!asyncResult.IsCompleted && !cancellationTokenSource.Token.IsCancellationRequested)
                                        {
                                            await ProcessStreamOutput(outputReader, errorReader, logBox, cancellationTokenSource.Token);
                                            await Task.Delay(100, cancellationTokenSource.Token);
                                        }

                                        if (!cancellationTokenSource.Token.IsCancellationRequested)
                                        {
                                            // 完成命令执行
                                            deployCmd.EndExecute(asyncResult);

                                            // 读取剩余的输出
                                            await ProcessStreamOutput(outputReader, errorReader, logBox, cancellationTokenSource.Token);
                                        }

                                        return deployCmd.ExitStatus;
                                    }
                                    catch (Exception)
                                    {
                                        // 确保命令被终止
                                        try 
                                        { 
                                            if (asyncResult != null && !asyncResult.IsCompleted)
                                            {
                                                deployCmd.EndExecute(asyncResult);
                                            }
                                        } 
                                        catch { }
                                        throw;
                                    }
                                });

                                try
                                {
                                    // 等待命令执行完成或取消
                                    await Task.WhenAny(executeTask, Task.Delay(-1, cancellationTokenSource.Token));

                                    if (cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        try
                                        {
                                            var killCmd = sshClient.CreateCommand($"pkill -f {txtDeployScript.Text}");
                                            killCmd.Execute();
                                        }
                                        catch { }
                                        SafeAppendText(logBox, "\n部署已取消\n", Color.Yellow);
                                        return;
                                    }

                                    var exitCode = await executeTask;
                                    if (exitCode != 0)
                                    {
                                        SafeAppendText(logBox, $"\n部署脚本执行失败，退出代码: {exitCode}\n", Color.Red);
                                        return;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SafeAppendText(logBox, $"\n执行脚本时发生错误: {ex.Message}\n", Color.Red);
                                    return;
                                }
                            }
                        }

                        SafeAppendText(logBox, "\n部署完成！\n", Color.Green);

                        if (logForm.InvokeRequired)
                        {
                            logForm.Invoke(new Action(() =>
                            {
                                MessageBox.Show("部署成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.Close();
                            }));
                        }
                        else
                        {
                            MessageBox.Show("部署成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        SafeAppendText(logBox, "\n操作已取消\n", Color.Yellow);
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText(logBox, $"\n发生错误: {ex.Message}\n", Color.Red);
                    }
                }, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"部署失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:F2} {sizes[order]}";
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}时{timeSpan.Minutes}分";
            }
            if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes}分{timeSpan.Seconds}秒";
            }
            return $"{timeSpan.Seconds}秒";
        }

        private void SafeAppendText(RichTextBox logBox, string text, Color? color = null)
        {
            if (logBox.IsDisposed) return;

            logBox.BeginInvoke(new Action(() =>
            {
                if (color.HasValue)
                {
                    logBox.SelectionColor = color.Value;
                }
                logBox.AppendText(text);
                if (color.HasValue)
                {
                    logBox.SelectionColor = logBox.ForeColor;
                }
                logBox.ScrollToCaret();
            }));
        }

        private async Task ProcessStreamOutput(StreamReader outputReader, StreamReader errorReader, RichTextBox logBox, CancellationToken cancellationToken)
        {
            while (!outputReader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await outputReader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    SafeAppendText(logBox, line + Environment.NewLine);
                }
            }

            while (!errorReader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await errorReader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    SafeAppendText(logBox, line + Environment.NewLine, Color.Red);
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