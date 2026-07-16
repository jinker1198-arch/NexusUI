using System;
using System.Drawing;
using System.Windows.Forms;

namespace NexusUI.Core;

/// <summary>
/// Tracks mouse position and click state for hit-testing GDI+ controls.
/// </summary>
internal static class MouseTracker
{
    public static Point Position { get; private set; }
    public static bool LeftPressed { get; private set; }
    public static bool LeftClicked { get; private set; }
    public static Point LastClickPoint { get; private set; }

    public static void Attach(Form form)
    {
        form.MouseMove += (_, e) => Position = e.Location;
        form.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                LeftPressed = true;
                LeftClicked = true;
                LastClickPoint = e.Location;
            }
        };
        form.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                LeftPressed = false;
        };
    }

    /// <summary>Call at end of each frame to consume the click event.</summary>
    public static void EndFrame()
    {
        LeftClicked = false;
    }
}
