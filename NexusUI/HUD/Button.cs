using System;
using System.Drawing;
using System.Windows.Forms;

namespace NexusUI.HUD;

public class Button : HudElement
{
    public string Text { get; set; } = "Button";
    public Color FillColor { get; set; } = Color.FromArgb(60, 160, 240);
    public Color HoverColor { get; set; } = Color.FromArgb(80, 180, 255);
    public Color TextColor { get; set; } = Color.White;

    public event Action? Clicked;

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible) return;

        var rect = new Rectangle(Position, Size);
        bool hovered = rect.Contains(CursorPosition);

        using var brush = new SolidBrush(hovered ? HoverColor : FillColor);
        g.FillRectangle(brush, rect);

        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        TextRenderer.DrawText(g, Text, font, rect, TextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static Point CursorPosition => System.Windows.Forms.Cursor.Position;
}
