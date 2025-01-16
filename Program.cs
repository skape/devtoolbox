using System;
using System.Windows.Forms;
using DevToolbox.Forms;

namespace DevToolbox
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0] == "--cleanup")
            {
                // 直接执行清理
                var mainForm = new MainForm();
                mainForm.ExecuteCleanup();
            }
            else
            {
                Application.Run(new MainForm());
            }
        }
    }
}