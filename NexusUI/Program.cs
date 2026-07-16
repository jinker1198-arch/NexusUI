using System;
using System.Windows.Forms;
using NexusUI.Core;

namespace NexusUI;

internal static class Program
{
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            using var app = new NexusApplication();
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"NexusUI encountered an error:\n\n{ex.Message}\n\n{ex.GetType().Name}",
                "NexusUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
