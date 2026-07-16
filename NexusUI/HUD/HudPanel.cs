using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NexusUI.HUD;

public class HudPanel : HudElement
{
    public Color BackgroundColor { get; set; } = Color.FromArgb(180, 25, 25, 30);
    public Color BorderColor { get; set; } = Color.FromArgb(80, 60, 160, 240);
    public float BorderThickness { get; set; } = 1f;
    public string? Title { get; set; }

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible) return;

        var rect = new Rectangle(Position, Size);

        using var bgBrush = new SolidBrush(BackgroundColor);
        g.FillRectangle(bgBrush, rect);

        if (BorderThickness > 0)
        {
            using var borderPen = new Pen(BorderColor, BorderThickness);
            g.DrawRectangle(borderPen, rect);
        }

        if (!string.IsNullOrEmpty(Title))
        {
            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            TextRenderer.DrawText(g, Title, titleFont,
                new Rectangle(Position.X + 6, Position.Y + 3, Size.Width - 12, 20),
                Color.FromArgb(230, 230, 235));
        }
    }
}
