using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NexusUI.HUD;

public class StatDisplay : HudElement
{
    private readonly List<(string Label, string Value)> _stats = new();
    public Color ValueColor { get; set; } = Color.FromArgb(235, 235, 240);
    public float LineSpacing { get; set; } = 2f;

    public void SetStat(string label, string value)
    {
        for (int i = 0; i < _stats.Count; i++)
        {
            if (_stats[i].Label == label)
            {
                _stats[i] = (label, value);
                return;
            }
        }
        _stats.Add((label, value));
    }

    public void RemoveStat(string label)
    {
        _stats.RemoveAll(s => s.Label == label);
    }

    public void ClearStats() => _stats.Clear();

    public override void Render(Graphics g, Rectangle viewport)
    {
        if (!Visible || _stats.Count == 0) return;

        using var font = new Font("Consolas", 10);
        int y = Position.Y;

        foreach (var (label, value) in _stats)
        {
            string text = $"{label}: {value}";
            TextRenderer.DrawText(g, text, font,
                new Rectangle(Position.X, y, Size.Width, (int)(font.GetHeight() + LineSpacing)),
                ValueColor, TextFormatFlags.Left);
            y += (int)(font.GetHeight() + LineSpacing);
        }
    }
}
