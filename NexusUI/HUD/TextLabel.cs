using System;
using System.Drawing;
using System.Windows.Forms;

namespace NexusUI.HUD;

public class TextLabel : HudElement
{
    public string Text { get; set; } = string.Empty;
    public Color TextColor { get; set; } = Color.FromArgb(235, 235, 240);
    public float FontSize { get; set; } = 11f;
    public bool CenterX { get; set; }
    public bool CenterY { get; set; }
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        using var font = new Font("Segoe UI", FontSize, FontStyle);

        var textSize = TextRenderer.MeasureText(Text, font);
        int x = Position.X;
        int y = Position.Y;

        if (CenterX) x += (Size.Width - textSize.Width) / 2;
        if (CenterY) y += (Size.Height - textSize.Height) / 2;

        TextRenderer.DrawText(g, Text, font,
            new Rectangle(x, y, Size.Width, Size.Height),
            TextColor, TextFormatFlags.Left);
    }
}
