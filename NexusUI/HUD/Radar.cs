using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using NexusUI.Shared;

namespace NexusUI.HUD;

internal sealed class Radar : HudElement
{
    private GameStateSnapshot _state;
    private readonly object _lock = new();

    public int Radius { get; set; } = 120;
    public float WorldScale { get; set; } = 0.04f; // pixels per game unit
    public Color BackgroundColor { get; set; } = Color.FromArgb(180, 8, 8, 12);
    public Color BorderColor { get; set; } = Color.FromArgb(80, 160, 240);
    public Color TeammateColor { get; set; } = Color.FromArgb(80, 220, 130);
    public Color EnemyColor { get; set; } = Color.FromArgb(230, 90, 90);
    public Color LocalColor { get; set; } = Color.FromArgb(60, 180, 255);

    public void UpdateState(GameStateSnapshot state)
    {
        lock (_lock) { _state = state; }
    }

    public override void Render(Graphics g, Rectangle viewport)
    {
        lock (_lock)
        {
            if (!_state.IsConnected) return;

            int cx = Position.X + Radius;
            int cy = Position.Y + Radius;
            var localPos = _state.LocalPlayer.Position;

            // Radar background
            using var bgBrush = new SolidBrush(BackgroundColor);
            g.FillEllipse(bgBrush, Position.X, Position.Y, Radius * 2, Radius * 2);

            using var borderPen = new Pen(BorderColor, 1.5f);
            g.DrawEllipse(borderPen, Position.X, Position.Y, Radius * 2, Radius * 2);

            // Grid lines
            using var gridPen = new Pen(Color.FromArgb(30, 100, 100, 110), 1);
            g.DrawEllipse(gridPen, cx - Radius / 2, cy - Radius / 2, Radius, Radius);
            g.DrawLine(gridPen, cx - Radius, cy, cx + Radius, cy);
            g.DrawLine(gridPen, cx, cy - Radius, cx, cy + Radius);

            // Player dots
            for (int i = 0; i < _state.PlayerCount; i++)
            {
                var player = _state.Players[i];
                float dx = (player.Position.X - localPos.X) * WorldScale;
                float dy = (player.Position.Y - localPos.Y) * WorldScale;

                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > Radius - 6) continue;

                int px = cx + (int)dx;
                int py = cy + (int)dy;

                bool isEnemy = player.Team != _state.LocalPlayer.Team;
                using var dotBrush = new SolidBrush(isEnemy ? EnemyColor : TeammateColor);
                g.FillEllipse(dotBrush, px - 4, py - 4, 8, 8);
            }

            // Local player center dot
            using var localBrush = new SolidBrush(LocalColor);
            g.FillEllipse(localBrush, cx - 5, cy - 5, 10, 10);
            using var localBorder = new Pen(Color.FromArgb(200, 200, 210), 1.5f);
            g.DrawEllipse(localBorder, cx - 5, cy - 5, 10, 10);

            // Status text
            using var infoFont = new Font("Consolas", 7f);
            using var infoBrush = new SolidBrush(Color.FromArgb(120, 122, 128));
            g.DrawString($"{_state.PlayerCount} players", infoFont, infoBrush,
                Position.X + 4, Position.Y + Radius * 2 + 2);
        }
    }
}
