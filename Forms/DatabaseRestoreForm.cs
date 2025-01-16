using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevToolbox.Forms
{
    public partial class DatabaseRestoreForm : Form
    {
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtMySqlPath;
        private TextBox txtSelectedFile;
        private TextBox txtDatabase;
        private Button btnBrowseMySql;
        private Button btnBrowseFile;
        private Button btnRestore;
        private Button btnCancel;

        public DatabaseRestoreForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "数据库恢复";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // IP地址
            var lblIp = new Label
            {
                Text = "数据库IP:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 23)
            };
            txtIp = new TextBox
            {
                Text = "localhost",
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(200, 23)
            };

            // 端口
            var lblPort = new Label
            {
                Text = "端口:",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(100, 23)
            };
            txtPort = new TextBox
            {
                Text = "3306",
                Location = new System.Drawing.Point(130, 60),
                Size = new System.Drawing.Size(200, 23)
            };

            // 用户名
            var lblUsername = new Label
            {
                Text = "用户名:",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(100, 23)
            };
            txtUsername = new TextBox
            {
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(200, 23)
            };

            // 密码
            var lblPassword = new Label
            {
                Text = "密码:",
                Location = new System.Drawing.Point(20, 140),
                Size = new System.Drawing.Size(100, 23)
            };
            txtPassword = new TextBox
            {
                PasswordChar = '*',
                Location = new System.Drawing.Point(130, 140),
                Size = new System.Drawing.Size(200, 23)
            };

            // 数据库名称
            var lblDatabase = new Label
            {
                Text = "数据库名称:",
                Location = new Point(20, 180),
                Size = new Size(100, 23)
            };
            txtDatabase = new TextBox
            {
                Location = new Point(130, 180),
                Size = new Size(200, 23)
            };

            // MySQL路径
            var lblMySqlPath = new Label
            {
                Text = "MySQL路径:",
                Location = new Point(20, 220),
                Size = new Size(100, 23)
            };
            txtMySqlPath = new TextBox
            {
                Text = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
                Location = new Point(130, 220),
                Size = new Size(250, 23)
            };
            btnBrowseMySql = new Button
            {
                Text = "浏览",
                Location = new Point(390, 220),
                Size = new Size(75, 23)
            };
            btnBrowseMySql.Click += BtnBrowseMySql_Click;

            // SQL文件选择
            var lblFile = new Label
            {
                Text = "SQL文件:",
                Location = new Point(20, 260),
                Size = new Size(100, 23)
            };
            txtSelectedFile = new TextBox
            {
                ReadOnly = true,
                Location = new Point(130, 260),
                Size = new Size(250, 23)
            };
            btnBrowseFile = new Button
            {
                Text = "浏览",
                Location = new Point(390, 260),
                Size = new Size(75, 23)
            };
            btnBrowseFile.Click += BtnBrowseFile_Click;

            // 按钮
            btnRestore = new Button
            {
                Text = "开始恢复",
                Location = new Point(130, 300),
                Size = new Size(100, 30)
            };
            btnRestore.Click += BtnRestore_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(250, 300),
                Size = new Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            // 添加控件
            Controls.AddRange(new Control[] {
                lblIp, txtIp,
                lblPort, txtPort,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblDatabase, txtDatabase,
                lblMySqlPath, txtMySqlPath, btnBrowseMySql,
                lblFile, txtSelectedFile, btnBrowseFile,
                btnRestore, btnCancel
            });
        }

        private void BtnBrowseMySql_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "MySQL执行文件|mysql.exe|所有文件|*.*";
                dialog.InitialDirectory = @"C:\Program Files\MySQL\MySQL Server 8.0\bin";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtMySqlPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "SQL文件|*.sql|所有文件|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtSelectedFile.Text = dialog.FileName;
                }
            }
        }

        private async void BtnRestore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSelectedFile.Text))
            {
                MessageBox.Show("请选择要恢复的SQL文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtDatabase.Text))
            {
                MessageBox.Show("请输入要恢复的数据库名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtMySqlPath.Text))
            {
                MessageBox.Show("MySQL执行文件路径无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 获取SQL文件大小用于计算进度
                var fileInfo = new FileInfo(txtSelectedFile.Text);
                long totalSize = fileInfo.Length;
                
                var loadingForm = new Utils.LoadingForm(this, "正在恢复数据库...", true);
                loadingForm.Show();

                // 切换到MySQL所在目录
                string mysqlDir = Path.GetDirectoryName(txtMySqlPath.Text);
                
                // 构建命令，使用标准格式，密码用引号包裹
                string command = $"cd /d \"{mysqlDir}\" && mysql -h {txtIp.Text} -P {txtPort.Text} -u {txtUsername.Text} -p\"{txtPassword.Text}\" {txtDatabase.Text} < \"{txtSelectedFile.Text}\"";

                // 创建取消令牌
                using (var cts = new System.Threading.CancellationTokenSource())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = mysqlDir
                    };

                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        // 启动进程
                        process.Start();

                        // 创建进度更新定时器
                        System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer();
                        progressTimer.Interval = 100; // 每100ms更新一次
                        
                        // 用于跟踪已处理的数据大小
                        long processedSize = 0;
                        DateTime startTime = DateTime.Now;

                        progressTimer.Tick += (s, args) =>
                        {
                            if (process.HasExited) return;

                            // 估算进度
                            TimeSpan elapsed = DateTime.Now - startTime;
                            if (elapsed.TotalSeconds > 0)
                            {
                                // 估算已处理的数据量
                                processedSize = Math.Min(processedSize + 1024 * 1024, totalSize); // 假设每秒处理1MB
                                double progress = (double)processedSize / totalSize * 100;
                                
                                // 计算速度
                                double speed = processedSize / elapsed.TotalSeconds;
                                
                                // 更新进度显示
                                loadingForm.UpdateProgress(
                                    processedSize,
                                    totalSize,
                                    $"已处理: {FormatSize(processedSize)}/{FormatSize(totalSize)} ({progress:F1}%) - {FormatSize((long)speed)}/s"
                                );
                            }
                        };

                        progressTimer.Start();

                        // 异步读取输出
                        var outputTask = process.StandardOutput.ReadToEndAsync();
                        var errorTask = process.StandardError.ReadToEndAsync();

                        // 等待进程完成
                        await Task.Run(() => process.WaitForExit());

                        // 停止进度更新
                        progressTimer.Stop();
                        progressTimer.Dispose();

                        // 获取输出结果
                        string error = await errorTask;
                        string output = await outputTask;

                        loadingForm.CleanUp();

                        if (process.ExitCode != 0 || (!string.IsNullOrEmpty(error) && !error.Contains("Warning")))
                        {
                            if (error.Contains("Access denied"))
                            {
                                MessageBox.Show("数据库访问被拒绝，请检查用户名和密码是否正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show($"数据库恢复失败:\n{error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("数据库恢复成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatSize(long bytes)
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
    }
} 