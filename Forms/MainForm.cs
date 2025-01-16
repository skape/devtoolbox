using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;
using DevToolbox.Utils;

namespace DevToolbox.Forms
{
    public class MainForm : Form
    {
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblCPU;
        private ToolStripStatusLabel lblMemory;
        private System.Windows.Forms.Timer resourceMonitor;
        private PerformanceCounter cpuCounter;

        public MainForm()
        {
            InitializeComponent();
            InitializeStatusBar();
            StartResourceMonitoring();
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            this.Text = "开发工具箱";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 242, 245);  // 设置窗体背景色

            // 添加标题标签
            var lblTitle = new Label
            {
                Text = "开发工具箱",
                Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 144, 255),
                AutoSize = true,
                Location = new Point(
                    (this.ClientSize.Width - 100) / 2,  // 大致居中
                    20
                )
            };
            this.Controls.Add(lblTitle);

            // 创建功能按钮
            var btnSSHDocker = CreateButton("SSH + Docker", 0);
            var btnDBRestore = CreateButton("数据库恢复", 1);
            var btnDeploy = CreateButton("部署", 2);
            var btnCleanup = CreateButton("清理", 3);

            // 添加按钮点击事件
            btnSSHDocker.Click += BtnSSHDocker_Click;
            btnDBRestore.Click += BtnDBRestore_Click;
            btnDeploy.Click += BtnDeploy_Click;
            btnCleanup.Click += BtnCleanup_Click;

            // 添加控件
            this.Controls.AddRange(new Control[] { 
                btnSSHDocker, 
                btnDBRestore, 
                btnDeploy, 
                btnCleanup 
            });
        }

        private Button CreateButton(string text, int index)
        {
            int buttonWidth = 200;
            int buttonHeight = 60;
            int margin = 30;
            int startX = (this.ClientSize.Width - (buttonWidth * 2 + margin)) / 2;
            int startY = 80;  // 稍微上移

            var button = new Button
            {
                Text = text,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(
                    startX + (index % 2) * (buttonWidth + margin),
                    startY + (index / 2) * (buttonHeight + margin)
                ),
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Regular),  // 使用雅黑字体
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 144, 255),    // 使用蓝色主题
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TabIndex = index
            };

            // 移除按钮边框
            button.FlatAppearance.BorderSize = 0;
            
            // 添加鼠标悬停效果
            button.MouseEnter += (s, e) => {
                button.BackColor = Color.FromArgb(64, 169, 255);  // 稍亮的蓝色
                button.Font = new Font(button.Font, FontStyle.Bold);
            };
            
            button.MouseLeave += (s, e) => {
                button.BackColor = Color.FromArgb(24, 144, 255);  // 恢复原色
                button.Font = new Font(button.Font, FontStyle.Regular);
            };

            // 添加点击效果
            button.MouseDown += (s, e) => {
                button.BackColor = Color.FromArgb(9, 109, 217);  // 深蓝色
            };
            
            button.MouseUp += (s, e) => {
                button.BackColor = Color.FromArgb(24, 144, 255);  // 恢复原色
            };

            return button;
        }

        private void BtnSSHDocker_Click(object sender, EventArgs e)
        {
            var sshLoginForm = new SSHLoginForm();
            sshLoginForm.ShowDialog();
        }

        private void BtnDBRestore_Click(object sender, EventArgs e)
        {
            var dbRestoreForm = new DatabaseRestoreForm();
            dbRestoreForm.ShowDialog();
        }

        private void BtnDeploy_Click(object sender, EventArgs e)
        {
            MessageBox.Show("部署功能正在开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnCleanup_Click(object sender, EventArgs e)
        {
            // 如果不是管理员权限，重启程序
            if (!IsRunAsAdministrator())
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = "--cleanup"
                    };
                    Process.Start(startInfo);
                    Application.Exit();
                    return;
                }
                catch (Exception)
                {
                    MessageBox.Show("清理功能需要管理员权限。", "需要权限", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            const string RAMMAP_PATH = @"D:\software\RAMMap\RAMMap64.exe";
            if (!File.Exists(RAMMAP_PATH))
            {
                MessageBox.Show($"找不到 RAMMap64.exe，请检查路径：{RAMMAP_PATH}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var loadingForm = new LoadingForm(this, "正在清理系统内存...", true))
            {
                try
                {
                    loadingForm.Show();
                    Application.DoEvents();

                    // 开始监控内存使用情况
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += (s, args) =>
                    {
                        var memoryStatus = GetMemoryStatus();
                        double usedPercentage = 100 - (memoryStatus.availablePhysical * 100.0 / memoryStatus.totalPhysical);
                        loadingForm.UpdateProgress(
                            (long)(usedPercentage * 100),
                            10000,
                            $"内存使用: {usedPercentage:F1}% ({FormatSize(memoryStatus.availablePhysical)} 可用)"
                        );
                    };
                    timer.Start();

                    // 执行所有清理操作
                    string[] cleanupCommands = { "-Ew", "-Es", "-Em", "-Et", "-E0" };
                    int totalSteps = cleanupCommands.Length;
                    int currentStep = 0;

                    foreach (var command in cleanupCommands)
                    {
                        currentStep++;
                        loadingForm.Message = $"正在执行清理操作 ({currentStep}/{totalSteps})...";
                        
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = RAMMAP_PATH,
                                Arguments = command,
                                UseShellExecute = true,
                                Verb = "runas",
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            }
                        };
                        
                        process.Start();
                        await Task.Run(() => process.WaitForExit());
                    }

                    timer.Stop();
                    timer.Dispose();

                    loadingForm.Close();
                    MessageBox.Show("内存清理完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清理过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 检查是否以管理员身份运行
        private bool IsRunAsAdministrator()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;                   // 结构体大小
            public uint dwMemoryLoad;              // 内存使用百分比
            public ulong ullTotalPhys;             // 物理内存总量
            public ulong ullAvailPhys;             // 可用物理内存
            public ulong ullTotalPageFile;         // 页面文件总量
            public ulong ullAvailPageFile;         // 可用页面文件
            public ulong ullTotalVirtual;          // 虚拟内存总量
            public ulong ullAvailVirtual;          // 可用虚拟内存
            public ulong ullAvailExtendedVirtual;  // 保留，必须为0

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        private (long totalPhysical, long availablePhysical) GetMemoryStatus()
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (!GlobalMemoryStatusEx(memStatus))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return ((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMemoryStatus error: {ex.Message}");
                // 返回一个默认值，避免程序崩溃
                return (8L * 1024 * 1024 * 1024, 4L * 1024 * 1024 * 1024); // 假设8GB总内存，4GB可用
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        // 添加公共方法执行清理
        public async void ExecuteCleanup()
        {
            using (var loadingForm = new LoadingForm(this, "正在清理系统内存...", true))
            {
                try
                {
                    loadingForm.Show();
                    Application.DoEvents();

                    // 执行清理脚本
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = @"D:\software\RAMMap\clean.bat",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    };
                    process.Start();

                    // 开始监控内存使用情况
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += (s, args) =>
                    {
                        var memoryStatus = GetMemoryStatus();
                        double usedPercentage = 100 - (memoryStatus.availablePhysical * 100.0 / memoryStatus.totalPhysical);
                        loadingForm.UpdateProgress(
                            (long)(usedPercentage * 100),
                            10000,
                            $"内存使用: {usedPercentage:F1}% ({FormatSize(memoryStatus.availablePhysical)} 可用)"
                        );
                    };
                    timer.Start();

                    await Task.Run(() => process.WaitForExit());
                    timer.Stop();
                    timer.Dispose();

                    loadingForm.Close();
                    MessageBox.Show("内存清理完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();  // 清理完成后退出程序
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清理过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
        }

        private void InitializeStatusBar()
        {
            // 创建状态栏
            statusStrip = new StatusStrip
            {
                SizingGrip = false,
                BackColor = Color.FromArgb(33, 33, 33),  // 深色背景
                Padding = new Padding(2),
                Height = 28  // 增加高度
            };

            // CPU使用率标签
            lblCPU = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.RaisedInner,
                AutoSize = false,
                Width = 150,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,  // 白色文字
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(10, 0, 0, 0)  // 左侧padding
            };

            // 内存使用标签
            lblMemory = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.RaisedInner,
                AutoSize = false,
                Width = 300,  // 增加宽度以显示更多信息
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,  // 白色文字
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(10, 0, 0, 0)  // 左侧padding
            };

            // 添加标签到状态栏
            statusStrip.Items.AddRange(new ToolStripItem[] { lblCPU, lblMemory });

            // 添加状态栏到窗体
            this.Controls.Add(statusStrip);

            // 初始化CPU计数器
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        private void StartResourceMonitoring()
        {
            resourceMonitor = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };

            resourceMonitor.Tick += (s, e) =>
            {
                try
                {
                    // 更新CPU使用率
                    float cpuUsage = cpuCounter.NextValue();
                    string cpuColor = GetResourceColor(cpuUsage);
                    lblCPU.Text = $"CPU: {cpuUsage:F1}%";
                    lblCPU.ForeColor = ColorTranslator.FromHtml(cpuColor);

                    // 更新内存使用情况
                    var memoryStatus = GetMemoryStatus();
                    double memoryUsage = 100 - (memoryStatus.availablePhysical * 100.0 / memoryStatus.totalPhysical);
                    string memoryColor = GetResourceColor(memoryUsage);
                    string availableMemory = FormatSize(memoryStatus.availablePhysical);
                    string totalMemory = FormatSize(memoryStatus.totalPhysical);
                    lblMemory.Text = $"内存: {memoryUsage:F1}% ({availableMemory} 可用 / {totalMemory} 总计)";
                    lblMemory.ForeColor = ColorTranslator.FromHtml(memoryColor);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"资源监控错误: {ex.Message}");
                }
            };

            resourceMonitor.Start();
        }

        // 根据资源使用率返回对应的颜色
        private string GetResourceColor(double usage)
        {
            if (usage >= 90) return "#FF4444";      // 红色 (危险)
            if (usage >= 70) return "#FFBB33";      // 橙色 (警告)
            if (usage >= 50) return "#00C851";      // 绿色 (正常)
            return "#33B5E5";                       // 蓝色 (良好)
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // 清理资源
            if (resourceMonitor != null)
            {
                resourceMonitor.Stop();
                resourceMonitor.Dispose();
            }
            
            if (cpuCounter != null)
            {
                cpuCounter.Dispose();
            }
        }
    }
} 