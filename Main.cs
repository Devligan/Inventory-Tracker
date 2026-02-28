using System;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}