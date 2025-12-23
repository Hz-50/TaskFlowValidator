using System;
using System.Windows.Forms;

namespace DSAproject
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // IMPORTANT: This name must match your Form class name!
            // If your form file is Form1.cs, keep it as "new Form1()"
            // If you renamed it to MainForm.cs, change it to "new MainForm()"
            Application.Run(new MainForm());
        }
    }
}