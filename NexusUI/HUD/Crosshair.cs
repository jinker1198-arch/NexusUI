using System;
using System.Drawing;

namespace NexusUI.HUD;

public class Crosshair : HudElement
{
    public enum Style { Cross, Circle, Dot }

    public Style CrosshairStyle { get; set; } = Style.Cross;
    public float Thickness { get; set; } = 2f;
    public float Gap { get; set; } = 4f;
    public float Length { get; set; } = 12f;
    public float Radius { get; set; } = 6f;
    public Color CrosshairColor { get; set; } = Color.FromArgb(60, 160, 240);

    public Point ScreenCenter { get; set; }

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible) return;

        var center = ScreenCenter;
        if (center == Point.Empty)
            center = new Point(viewport.Width / 2, viewport.Height / 2);

        using var pen = new Pen(CrosshairColor, Thickness);
        using var fillBrush = new SolidBrush(CrosshairColor);

        switch (CrosshairStyle)
        {
            case Style.Cross:
                float gap = Gap, len = Length, thick = Thickness;
                g.DrawLine(pen, center.X, center.Y - gap - len, center.X, center.Y - gap);
                g.DrawLine(pen, center.X, center.Y + gap, center.X, center.Y + gap + len);
                g.DrawLine(pen, center.X - gap - len, center.Y, center.X - gap, center.Y);
                g.DrawLine(pen, center.X + gap, center.Y, center.X + gap + len, center.Y);
                g.FillEllipse(fillBrush, center.X - 1.5f, center.Y - 1.5f, 3, 3);
                break;

            case Style.Circle:
                g.DrawEllipse(pen, center.X - Radius, center.Y - Radius, Radius * 2, Radius * 2);
                g.FillEllipse(fillBrush, center.X - 1.5f, center.Y - 1.5f, 3, 3);
                break;

            case Style.Dot:
                g.FillEllipse(fillBrush, center.X - 3f, center.Y - 3f, 6, 6);
                break;
        }
    }
}
