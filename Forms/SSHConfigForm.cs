using System;
using System.Drawing;
using System.Windows.Forms;
using DevToolbox.Models;

namespace DevToolbox.Forms
{
    public class SSHConfigForm : Form
    {
        private TextBox txtName;
        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnSave;
        private Button btnCancel;

        public SSHConfig Config { get; private set; }

        public SSHConfigForm(SSHConfig config = null)
        {
            Config = config ?? new SSHConfig();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = Config.Id == 0 ? "添加SSH服务器" : "编辑SSH服务器";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 创建输入控件
            var lblName = new Label { Text = "名称:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(200, 23),
                Text = Config.Name
            };

            var lblHost = new Label { Text = "主机:", Location = new Point(20, 50), AutoSize = true };
            txtHost = new TextBox
            {
                Location = new Point(120, 50),
                Size = new Size(200, 23),
                Text = Config.Host
            };

            var lblPort = new Label { Text = "端口:", Location = new Point(20, 80), AutoSize = true };
            txtPort = new TextBox
            {
                Location = new Point(120, 80),
                Size = new Size(200, 23),
                Text = Config.Port == 0 ? "22" : Config.Port.ToString()
            };

            var lblUsername = new Label { Text = "用户名:", Location = new Point(20, 110), AutoSize = true };
            txtUsername = new TextBox
            {
                Location = new Point(120, 110),
                Size = new Size(200, 23),
                Text = Config.Username
            };

            var lblPassword = new Label { Text = "密码:", Location = new Point(20, 140), AutoSize = true };
            txtPassword = new TextBox
            {
                Location = new Point(120, 140),
                Size = new Size(200, 23),
                PasswordChar = '*',
                Text = Config.Password
            };

            btnSave = new Button
            {
                Text = "保存",
                DialogResult = DialogResult.OK,
                Location = new Point(120, 200),
                Size = new Size(80, 30)
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(220, 200),
                Size = new Size(80, 30)
            };

            this.Controls.AddRange(new Control[]
            {
                lblName, txtName,
                lblHost, txtHost,
                lblPort, txtPort,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                btnSave, btnCancel
            });

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                    throw new Exception("请输入服务器名称");

                if (string.IsNullOrWhiteSpace(txtHost.Text))
                    throw new Exception("请输入主机地址");

                if (!int.TryParse(txtPort.Text, out int port))
                    throw new Exception("端口号必须是有效的数字");

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                    throw new Exception("请输入用户名");

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                    throw new Exception("请输入密码");

                Config.Name = txtName.Text.Trim();
                Config.Host = txtHost.Text.Trim();
                Config.Port = port;
                Config.Username = txtUsername.Text.Trim();
                Config.Password = txtPassword.Text;
                Config.LastUsed = DateTime.Now;

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }
} 