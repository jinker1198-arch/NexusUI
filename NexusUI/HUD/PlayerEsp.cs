using System;
using System.Drawing;
using NexusUI.Shared;

namespace NexusUI.HUD;

internal sealed class PlayerEsp : HudElement
{
    private GameStateSnapshot _state;
    private readonly object _lock = new();

    private static readonly Font NameFont = new("Segoe UI", 8f, FontStyle.Bold);
    private static readonly Font HpFont = new("Segoe UI", 6.5f, FontStyle.Regular);

    private static readonly Color AllyColor = Color.FromArgb(80, 220, 130);
    private static readonly Color EnemyColor = Color.FromArgb(230, 90, 90);
    private static readonly Color BoxColor = Color.FromArgb(180, 180, 190);
    private static readonly Color SkeletonColor = Color.FromArgb(220, 220, 225);
    private static readonly Color BgColor = Color.FromArgb(140, 8, 8, 12);

    private static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (2, 3),       // spine
        (1, 4), (4, 5), (5, 6),       // left arm
        (1, 7), (7, 8), (8, 9),       // right arm
        (3, 10), (10, 11),             // left leg
        (3, 12), (12, 13),             // right leg
    };

    public void UpdateState(GameStateSnapshot state)
    {
        lock (_lock) { _state = state; }
    }

    public override void Render(Graphics g, Rectangle viewport)
    {
        lock (_lock)
        {
            if (!_state.IsConnected) return;

            for (int i = 0; i < _state.PlayerCount; i++)
            {
                var p = _state.Players[i];
                if (p.Health <= 0) continue;
                DrawPlayer(g, p);
            }
        }
    }

    private static void DrawPlayer(Graphics g, PlayerData p)
    {
        bool enemy = p.Team != 2; // 2 = CT, 3 = T; local team determines ally/enemy
        Color teamColor = enemy ? EnemyColor : AllyColor;

        // --- Skeleton ---
        if (p.ScreenBones.Length > 10)
        {
            using var skelPen = new Pen(SkeletonColor, 1.5f);
            foreach (var (a, b) in BonePairs)
            {
                if (a >= p.ScreenBones.Length || b >= p.ScreenBones.Length) continue;
                var p1 = p.ScreenBones[a];
                var p2 = p.ScreenBones[b];
                if (p1.X < 1 || p2.X < 1) continue;
                g.DrawLine(skelPen, p1.X, p1.Y, p2.X, p2.Y);
            }
        }

        // --- Box ---
        var box = p.ScreenBox;
        var rect = new System.Drawing.RectangleF(box.X, box.Y, box.Width, box.Height);
        if (rect.Height < 10 || rect.Width < 4) return;

        using var boxPen = new Pen(teamColor, 1.5f);
        g.DrawRectangle(boxPen, rect.X, rect.Y, rect.Width, rect.Height);

        // --- Health bar ---
        float hpFrac = Math.Clamp(p.Health / (float)p.MaxHealth, 0f, 1f);
        int barH = (int)(rect.Height * hpFrac);
        int barX = (int)(rect.Left - 5);
        int barY = (int)(rect.Bottom - barH);

        using var hpBg = new SolidBrush(Color.FromArgb(160, 20, 20, 26));
        g.FillRectangle(hpBg, barX - 1, rect.Y - 1, 4, rect.Height + 2);

        using var hpBrush = new SolidBrush(hpFrac > 0.5f ? AllyColor : hpFrac > 0.25f ? Color.FromArgb(230, 190, 50) : EnemyColor);
        g.FillRectangle(hpBrush, barX, barY, 2, barH);

        // HP text
        using var hpTxtBrush = new SolidBrush(Color.FromArgb(200, 210, 215));
        g.DrawString($"{p.Health}", HpFont, hpTxtBrush, barX - 6, rect.Y - 12);

        // --- Name ---
        using var nameBrush = new SolidBrush(teamColor);
        g.DrawString(p.Name, NameFont, nameBrush, (rect.Left + rect.Right) * 0.5f - 15, rect.Bottom + 2);
    }

}
