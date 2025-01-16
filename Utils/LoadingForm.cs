using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevToolbox.Utils
{
    public class LoadingForm : Form
    {
        private Label lblMessage;
        private Label lblProgress;
        private ProgressBar progressBar;
        private Form owner;

        public LoadingForm(Form owner, string message = "正在加载...", bool showProgress = false)
        {
            this.owner = owner;
            InitializeComponent(showProgress);
            lblMessage.Text = message;
            CenterToOwner();
        }

        private void InitializeComponent(bool showProgress)
        {
            // 设置窗体属性
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(300, showProgress ? 130 : 100);
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // 创建消息标签
            lblMessage = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 15),
                Size = new Size(280, 30),
                Font = new Font(this.Font.FontFamily, 10)
            };

            // 创建进度条
            progressBar = new ProgressBar
            {
                Style = showProgress ? ProgressBarStyle.Blocks : ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(10, 55),
                Size = new Size(280, 25)
            };

            // 创建进度信息标签
            if (showProgress)
            {
                lblProgress = new Label
                {
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(10, 85),
                    Size = new Size(280, 30),
                    Font = new Font(this.Font.FontFamily, 9)
                };
                this.Controls.Add(lblProgress);
            }

            // 添加控件
            this.Controls.Add(lblMessage);
            this.Controls.Add(progressBar);
        }

        public void UpdateProgress(long current, long total, string customMessage = null)
        {
            if (lblProgress != null && progressBar != null)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateProgress(current, total, customMessage)));
                    return;
                }

                double percentage = (double)current / total * 100;
                progressBar.Value = (int)percentage;

                if (customMessage != null)
                {
                    lblProgress.Text = customMessage;
                }
                else
                {
                    string speed = FormatSpeed(current);
                    string size = FormatSize(current) + " / " + FormatSize(total);
                    lblProgress.Text = $"{percentage:F1}% ({size}) - {speed}/s";
                }
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

        private string FormatSpeed(long bytes)
        {
            return FormatSize(bytes);
        }

        private void CenterToOwner()
        {
            if (owner != null)
            {
                this.Location = new Point(
                    owner.Location.X + (owner.Width - this.Width) / 2,
                    owner.Location.Y + (owner.Height - this.Height) / 2
                );
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        public string Message
        {
            get { return lblMessage.Text; }
            set
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => Message = value));
                    return;
                }
                lblMessage.Text = value;
            }
        }
    }
} 