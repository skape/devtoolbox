﻿namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(186, 239);
            button1.Name = "button1";
            button1.Size = new Size(200, 46);
            button1.TabIndex = 0;
            button1.Text = "loginSSH";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(186, 161);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(200, 38);
            textBox1.TabIndex = 1;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(186, 81);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(200, 38);
            textBox2.TabIndex = 2;
            textBox2.TextChanged += textBox2_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(62, 90);
            label1.Name = "label1";
            label1.Size = new Size(86, 31);
            label1.TabIndex = 3;
            label1.Text = "用户名";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(62, 169);
            label2.Name = "label2";
            label2.Size = new Size(62, 31);
            label2.TabIndex = 4;
            label2.Text = "密码";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "sshForm";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
    }
}
