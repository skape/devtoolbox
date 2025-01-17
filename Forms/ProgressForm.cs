using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevToolbox.Forms
{
    public partial class ProgressForm : Form
    {
        private TableLayoutPanel mainPanel;
        private ProgressBar progressBar;
        private Label lblProgress;

        public ProgressForm(string title)
        {
            InitializeComponent(title);
        }

        private void InitializeComponent(string title)
        {
            this.Text = title;
            this.Size = new Size(500, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 25,
                Margin = new Padding(0, 0, 0, 10)
            };

            lblProgress = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9F),
                Text = "准备中...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            mainPanel.Controls.Add(progressBar, 0, 0);
            mainPanel.Controls.Add(lblProgress, 0, 1);

            this.Controls.Add(mainPanel);
        }

        public void UpdateProgress(int percentage, string status)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action(() => UpdateProgressInternal(percentage, status)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
            else
            {
                UpdateProgressInternal(percentage, status);
            }
        }

        private void UpdateProgressInternal(int percentage, string status)
        {
            if (this.IsDisposed) return;

            try
            {
                if (percentage >= 0 && percentage <= 100)
                {
                    progressBar.Value = percentage;
                }
                lblProgress.Text = status;
            }
            catch (Exception) { }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // 使用双缓冲
                return cp;
            }
        }
    }
} 