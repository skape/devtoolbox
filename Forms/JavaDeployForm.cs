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

                    process.OutputDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            logBox.BeginInvoke(new Action(() =>
                            {
                                logBox.AppendText(args.Data + Environment.NewLine);
                                logBox.ScrollToCaret();
                            }));
                        }
                    };

                    process.ErrorDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            logBox.BeginInvoke(new Action(() =>
                            {
                                logBox.SelectionColor = Color.Red;
                                logBox.AppendText(args.Data + Environment.NewLine);
                                logBox.SelectionColor = logBox.ForeColor;
                                logBox.ScrollToCaret();
                            }));
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode != 0)
                    {
                        logBox.BeginInvoke(new Action(() =>
                        {
                            logBox.SelectionColor = Color.Red;
                            logBox.AppendText("\nMaven打包失败！\n");
                            logBox.SelectionColor = logBox.ForeColor;
                        }));
                        return;
                    }

                    logBox.BeginInvoke(new Action(() =>
                    {
                        logBox.SelectionColor = Color.Green;
                        logBox.AppendText("\nMaven打包成功！准备上传文件...\n");
                        logBox.SelectionColor = logBox.ForeColor;
                    }));

                    // 查找生成的JAR文件
                    var targetDir = Path.Combine(txtProjectPath.Text, "target");
                    var jarFiles = Directory.GetFiles(targetDir, "*.jar")
                        .Where(f => !f.EndsWith("-sources.jar") && !f.EndsWith("-javadoc.jar"))
                        .ToList();

                    if (jarFiles.Count == 0)
                    {
                        logBox.BeginInvoke(new Action(() =>
                        {
                            logBox.SelectionColor = Color.Red;
                            logBox.AppendText("\n未找到生成的JAR文件\n");
                            logBox.SelectionColor = logBox.ForeColor;
                        }));
                        return;
                    }

                    var jarFile = jarFiles[0]; // 使用第一个找到的JAR文件

                    // 连接服务器并上传文件
                    logBox.BeginInvoke(new Action(() =>
                    {
                        logBox.AppendText("\n正在连接服务器...\n");
                    }));

                    using (var sshClient = new SshClient(selectedServer.Host, selectedServer.Port, selectedServer.Username, selectedServer.Password))
                    {
                        sshClient.Connect();

                        // 创建远程目录
                        logBox.BeginInvoke(new Action(() =>
                        {
                            logBox.AppendText($"\n创建远程目录: {txtRemotePath.Text}\n");
                        }));

                        var mkdirCmd = sshClient.CreateCommand($"mkdir -p {txtRemotePath.Text}");
                        mkdirCmd.Execute();

                        // 上传JAR文件
                        logBox.BeginInvoke(new Action(() =>
                        {
                            logBox.AppendText("\n正在上传JAR文件...\n");
                        }));

                        using (var scpClient = new ScpClient(sshClient.ConnectionInfo))
                        {
                            scpClient.Connect();

                            var fileInfo = new FileInfo(jarFile);
                            var fileName = Path.GetFileName(jarFile);
                            var remotePath = txtRemotePath.Text.TrimEnd('/') + "/" + fileName;
                            
                            logBox.BeginInvoke(new Action(() =>
                            {
                                logBox.AppendText($"\n目标路径: {remotePath}\n");
                            }));

                            var lastProgress = 0L;
                            var lastUpdateTime = DateTime.Now;
                            var startTime = DateTime.Now;
                            var lastBytes = 0L;
                            
                            // 创建进度窗口
                            Form progressForm = new Form
                            {
                                Text = "文件上传进度",
                                Size = new Size(500, 150),
                                FormBorderStyle = FormBorderStyle.FixedDialog,
                                StartPosition = FormStartPosition.CenterScreen,
                                MaximizeBox = false,
                                MinimizeBox = false,
                                BackColor = Color.FromArgb(30, 30, 30),
                                TopMost = true
                            };
                            
                            var progressBar = new ProgressBar
                            {
                                Location = new Point(20, 20),
                                Size = new Size(440, 25),
                                Style = ProgressBarStyle.Continuous,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 0
                            };
                            
                            var lblProgress = new Label
                            {
                                Location = new Point(20, 55),
                                Size = new Size(440, 40),
                                ForeColor = Color.White,
                                Font = new Font("Consolas", 9F),
                                TextAlign = ContentAlignment.MiddleLeft
                            };
                            
                            progressForm.Controls.AddRange(new Control[] { progressBar, lblProgress });
                            progressForm.Show();

                            scpClient.Uploading += (sender, args) =>
                            {
                                var now = DateTime.Now;
                                if ((now - lastUpdateTime).TotalMilliseconds > 100) // 每100ms更新一次
                                {
                                    var progress = (int)(args.Uploaded * 100 / fileInfo.Length);
                                    var timeElapsed = (now - startTime).TotalSeconds;
                                    var bytesPerSecond = args.Uploaded / timeElapsed;
                                    var remainingBytes = fileInfo.Length - args.Uploaded;
                                    var estimatedSecondsRemaining = remainingBytes / bytesPerSecond;
                                    
                                    // 计算瞬时速度
                                    var instantSpeed = (args.Uploaded - lastBytes) / (now - lastUpdateTime).TotalSeconds;
                                    
                                    if (progress > lastProgress)
                                    {
                                        progressForm.BeginInvoke(new Action(() =>
                                        {
                                            progressBar.Value = progress;
                                            lblProgress.Text = string.Format(
                                                "进度: {0}% ({1}/{2})\n速度: {3}/s\n预计剩余时间: {4}",
                                                progress,
                                                FormatFileSize(args.Uploaded),
                                                FormatFileSize(fileInfo.Length),
                                                FormatFileSize((long)instantSpeed),
                                                FormatTimeSpan(TimeSpan.FromSeconds(estimatedSecondsRemaining))
                                            );
                                        }));
                                        lastProgress = progress;
                                    }
                                    lastUpdateTime = now;
                                    lastBytes = args.Uploaded;
                                }
                            };

                            await Task.Run(() => scpClient.Upload(fileInfo, remotePath));
                            scpClient.Disconnect();

                            // 在UI线程上关闭进度窗口
                            if (!progressForm.IsDisposed)
                            {
                                if (progressForm.InvokeRequired)
                                {
                                    progressForm.Invoke(new Action(() =>
                                    {
                                        progressForm.Close();
                                        progressForm.Dispose();
                                    }));
                                }
                                else
                                {
                                    progressForm.Close();
                                    progressForm.Dispose();
                                }
                            }
                            
                            // 更新日志
                            if (logBox.InvokeRequired)
                            {
                                logBox.Invoke(new Action(() =>
                                {
                                    logBox.AppendText($"\n文件上传完成: {remotePath}\n");
                                    logBox.ScrollToCaret();
                                }));
                            }
                            else
                            {
                                logBox.AppendText($"\n文件上传完成: {remotePath}\n");
                                logBox.ScrollToCaret();
                            }
                        }

                        // 执行部署脚本
                        if (logBox.InvokeRequired)
                        {
                            logBox.Invoke(new Action(() =>
                            {
                                logBox.AppendText("\n正在执行部署脚本...\n");
                                logBox.AppendText($"脚本路径: {txtDeployScript.Text}\n");
                                logBox.ScrollToCaret();
                            }));
                        }
                        else
                        {
                            logBox.AppendText("\n正在执行部署脚本...\n");
                            logBox.AppendText($"脚本路径: {txtDeployScript.Text}\n");
                            logBox.ScrollToCaret();
                        }

                        // 确保脚本有执行权限
                        var chmodCmd = sshClient.CreateCommand($"chmod +x {txtDeployScript.Text}");
                        await Task.Run(() => chmodCmd.Execute());

                        // 执行脚本文件
                        var deployCmd = sshClient.CreateCommand($"bash {txtDeployScript.Text}");
                        var deployResult = await Task.Run(() => deployCmd.Execute());
                        var deployError = deployCmd.Error;

                        if (!string.IsNullOrEmpty(deployError))
                        {
                            if (logBox.InvokeRequired)
                            {
                                logBox.Invoke(new Action(() =>
                                {
                                    logBox.SelectionColor = Color.Red;
                                    logBox.AppendText($"\n部署脚本执行失败: {deployError}\n");
                                    logBox.SelectionColor = logBox.ForeColor;
                                    logBox.ScrollToCaret();
                                }));
                            }
                            else
                            {
                                logBox.SelectionColor = Color.Red;
                                logBox.AppendText($"\n部署脚本执行失败: {deployError}\n");
                                logBox.SelectionColor = logBox.ForeColor;
                                logBox.ScrollToCaret();
                            }
                            return;
                        }

                        if (!string.IsNullOrEmpty(deployResult))
                        {
                            if (logBox.InvokeRequired)
                            {
                                logBox.Invoke(new Action(() =>
                                {
                                    logBox.AppendText($"\n部署脚本输出:\n{deployResult}\n");
                                    logBox.ScrollToCaret();
                                }));
                            }
                            else
                            {
                                logBox.AppendText($"\n部署脚本输出:\n{deployResult}\n");
                                logBox.ScrollToCaret();
                            }
                        }

                        sshClient.Disconnect();

                        if (logBox.InvokeRequired)
                        {
                            logBox.Invoke(new Action(() =>
                            {
                                logBox.SelectionColor = Color.Green;
                                logBox.AppendText("\n部署完成！\n");
                                logBox.SelectionColor = logBox.ForeColor;
                                logBox.ScrollToCaret();
                            }));
                        }
                        else
                        {
                            logBox.SelectionColor = Color.Green;
                            logBox.AppendText("\n部署完成！\n");
                            logBox.SelectionColor = logBox.ForeColor;
                            logBox.ScrollToCaret();
                        }

                        MessageBox.Show("部署成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    logBox.BeginInvoke(new Action(() =>
                    {
                        logBox.SelectionColor = Color.Red;
                        logBox.AppendText($"\n发生错误: {ex.Message}\n");
                        logBox.SelectionColor = logBox.ForeColor;
                    }));
                }
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