using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevToolbox.Forms
{
    public class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            this.Text = "开发工具箱";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

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
            int startY = 100;

            return new Button
            {
                Text = text,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(
                    startX + (index % 2) * (buttonWidth + margin),
                    startY + (index / 2) * (buttonHeight + margin)
                ),
                Font = new Font(this.Font.FontFamily, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
        }

        private void BtnSSHDocker_Click(object sender, EventArgs e)
        {
            var sshLoginForm = new SSHLoginForm();
            sshLoginForm.ShowDialog();
        }

        private void BtnDBRestore_Click(object sender, EventArgs e)
        {
            MessageBox.Show("数据库恢复功能正在开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDeploy_Click(object sender, EventArgs e)
        {
            MessageBox.Show("部署功能正在开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCleanup_Click(object sender, EventArgs e)
        {
            MessageBox.Show("清理功能正在开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
} 