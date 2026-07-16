using System;
using System.Drawing;
using System.Windows.Forms;

namespace NexusUI.HUD;

public class ProgressBar : HudElement
{
    public float Value { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 100f;
    public Color FillColor { get; set; } = Color.FromArgb(60, 160, 240);
    public Color BackgroundColor { get; set; } = Color.FromArgb(100, 30, 30, 35);
    public bool Vertical { get; set; }
    public string? Label { get; set; }

    private float ClampedFraction
    {
        get
        {
            if (MaxValue <= MinValue) return 0f;
            return Math.Clamp((Value - MinValue) / (MaxValue - MinValue), 0f, 1f);
        }
    }

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible) return;

        var rect = new Rectangle(Position, Size);

        using var bgBrush = new SolidBrush(BackgroundColor);
        g.FillRectangle(bgBrush, rect);

        float frac = ClampedFraction;
        Rectangle fillRect;

        if (Vertical)
            fillRect = new Rectangle(Position.X, Position.Y + (int)(Size.Height * (1f - frac)),
                Size.Width, (int)(Size.Height * frac));
        else
            fillRect = new Rectangle(Position.X, Position.Y,
                (int)(Size.Width * frac), Size.Height);

        using var fillBrush = new SolidBrush(FillColor);
        g.FillRectangle(fillBrush, fillRect);

        if (!string.IsNullOrEmpty(Label))
        {
            using var font = new Font("Segoe UI", 9);
            TextRenderer.DrawText(g, Label, font,
                new Rectangle(Position.X + 4, Position.Y + 1, Size.Width - 8, Size.Height - 2),
                Color.FromArgb(220, 220, 225), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
