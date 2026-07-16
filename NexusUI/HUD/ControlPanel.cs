using System;
using System.Drawing;
using NexusUI.Core;
using NexusUI.Updates;

namespace NexusUI.HUD;

internal sealed class ControlPanel : IDisposable
{
    private readonly HudRenderer _owner;
    private bool _visible = true;
    private int _activeTab;
    private string _updateStatus = "";
    private Color _updateStatusColor = TextMuted;

    // Palette
    private static readonly Color Bg = Color.FromArgb(232, 12, 12, 16);
    private static readonly Color BgAlt = Color.FromArgb(200, 18, 18, 24);
    private static readonly Color Border = Color.FromArgb(40, 160, 240);
    private static readonly Color HeaderBg = Color.FromArgb(200, 16, 18, 24);
    private static readonly Color Accent = Color.FromArgb(60, 180, 255);
    private static readonly Color AccentDim = Color.FromArgb(90, 60, 160, 240);
    private static readonly Color TextPrimary = Color.FromArgb(225, 225, 230);
    private static readonly Color TextSecondary = Color.FromArgb(140, 142, 148);
    private static readonly Color TextMuted = Color.FromArgb(80, 82, 88);
    private static readonly Color TabActive = Color.FromArgb(60, 180, 255);
    private static readonly Color TabInactive = Color.FromArgb(100, 100, 108);
    private static readonly Color Green = Color.FromArgb(80, 220, 130);
    private static readonly Color Yellow = Color.FromArgb(230, 190, 50);
    private static readonly Color Red = Color.FromArgb(230, 90, 90);
    private static readonly Color SwitchOn = Color.FromArgb(60, 180, 255);
    private static readonly Color SwitchOff = Color.FromArgb(80, 80, 88);

    private static readonly Font TitleFont = new("Segoe UI", 12f, FontStyle.Bold);
    private static readonly Font TabFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    private static readonly Font ItemFont = new("Segoe UI", 9f, FontStyle.Regular);
    private static readonly Font MonoFont = new("Consolas", 9f, FontStyle.Regular);
    private static readonly Font SmallFont = new("Segoe UI", 8f, FontStyle.Regular);

    private const int PanelWidth = 300;
    private const int PanelHeight = 420;
    private const int TabHeight = 32;
    private const int ItemHeight = 28;
    private const int Pad = 12;

    private static readonly string[] TabNames = { "FEATURES", "OVERLAY", "SYSTEM" };

    public bool Visible { get => _visible; set => _visible = value; }

    public ControlPanel(HudRenderer owner)
    {
        _owner = owner;
    }

    public void SetUpdateStatus(string text, Color color)
    {
        _updateStatus = text;
        _updateStatusColor = color;
    }

    public void Render(Graphics g, Rectangle screenBounds)
    {
        if (!_visible) return;

        int x = screenBounds.Width - PanelWidth - 20;
        int y = 20;

        DrawPanel(g, x, y);

        // Footer hint
        using var hintBrush = new SolidBrush(TextMuted);
        g.DrawString("INSERT \u2194 toggle  \u00B7  END \u2192 quit", SmallFont,
            hintBrush, x + Pad, y + PanelHeight - 20);
    }

    private void DrawPanel(Graphics g, int x, int y)
    {
        // Shadow
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillRectangle(shadowBrush, x + 4, y + 4, PanelWidth, PanelHeight);

        // Main bg
        using var bgBrush = new SolidBrush(Bg);
        using var borderPen = new Pen(Border, 1f);
        g.FillRectangle(bgBrush, x, y, PanelWidth, PanelHeight);
        g.DrawRectangle(borderPen, x, y, PanelWidth, PanelHeight);

        // Header bar
        using var headerBrush = new SolidBrush(HeaderBg);
        g.FillRectangle(headerBrush, x + 1, y + 1, PanelWidth - 2, 40);

        // Title
        g.DrawString("NEXUSUI  v1.0", TitleFont, new SolidBrush(TextPrimary), x + Pad, y + 8);

        // Tabs
        int tabX = x + Pad;
        int tabY = y + 44;

        for (int i = 0; i < TabNames.Length; i++)
        {
            int tabW = (PanelWidth - Pad * 2) / TabNames.Length - 4;
            bool hovered = IsHovered(new Rectangle(tabX, tabY, tabW, TabHeight));
            bool active = i == _activeTab;

            using var tabBg = new SolidBrush(active ? AccentDim : Color.FromArgb(30, 30, 38));
            g.FillRectangle(tabBg, tabX, tabY, tabW, TabHeight);

            using var tabText = new SolidBrush(active ? TabActive : (hovered ? TextPrimary : TabInactive));
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(TabNames[i], TabFont, tabText, new Rectangle(tabX, tabY, tabW, TabHeight), format);

            if (active)
            {
                using var accentPen = new Pen(Accent, 2);
                g.DrawLine(accentPen, tabX + 4, tabY + TabHeight - 1, tabX + tabW - 4, tabY + TabHeight - 1);
            }

            if (hovered && MouseTracker.LeftClicked)
                _activeTab = i;

            tabX += tabW + 4;
        }

        // Content area
        int contentY = tabY + TabHeight + 4;
        DrawTabContent(g, x + Pad, contentY, PanelWidth - Pad * 2, _activeTab);
    }

    private void DrawTabContent(Graphics g, int x, int y, int w, int tab)
    {
        switch (tab)
        {
            case 0: DrawFeaturesTab(g, x, y, w); break;
            case 1: DrawOverlayTab(g, x, y, w); break;
            case 2: DrawSystemTab(g, x, y, w); break;
        }
    }

    private void DrawFeaturesTab(Graphics g, int x, int y, int w)
    {
        foreach (var elem in _owner.Elements)
        {
            string label = elem.GetType().Name switch
            {
                "Crosshair" => "Crosshair Overlay",
                "FpsCounter" => "FPS Counter",
                "HudPanel" => "Demo Panel",
                "TextLabel" => "Status Label",
                "ProgressBar" => "Health Bar",
                "ProgressBar2" => "Armor Bar",
                "StatDisplay" => "Stats Display",
                _ => elem.GetType().Name
            };
            DrawToggleRow(g, x, y, w, label, elem.Visible, v => elem.Visible = v, elem.Status);
            y += ItemHeight;
        }
    }

    private void DrawOverlayTab(Graphics g, int x, int y, int w)
    {
        g.DrawString("Display Settings", ItemFont, new SolidBrush(TextSecondary), x, y);
        y += 24;

        DrawLabel(g, x, y, w, "Opacity", "100%");
        y += 22;
        DrawSlider(g, x, y, w, 1f);
        y += 26;

        DrawLabel(g, x, y, w, "Crosshair Style", "Cross");
        y += 22;
        DrawSlider(g, x, y, w, 0.5f);
        y += 26;

        DrawToggleRow(g, x, y, w, "Snap to Game Window", true, _ => { }, HudElement.StatusLevel.Active);
    }

    private void DrawSystemTab(Graphics g, int x, int y, int w)
    {
        g.DrawString("Process Status", ItemFont, new SolidBrush(TextSecondary), x, y);
        y += 24;

        DrawStatusDot(g, x + 2, y + 4, Green);
        g.DrawString("DirectX Hook", MonoFont, new SolidBrush(TextPrimary), x + 18, y);
        g.DrawString("active", SmallFont, new SolidBrush(TextMuted), x + 130, y);
        y += 22;

        DrawStatusDot(g, x + 2, y + 4, Green);
        g.DrawString("Input Capture", MonoFont, new SolidBrush(TextPrimary), x + 18, y);
        g.DrawString("active", SmallFont, new SolidBrush(TextMuted), x + 130, y);
        y += 22;

        DrawStatusDot(g, x + 2, y + 4, Yellow);
        g.DrawString("Game Process", MonoFont, new SolidBrush(TextPrimary), x + 18, y);
        g.DrawString("waiting", SmallFont, new SolidBrush(TextMuted), x + 130, y);
        y += 22;

        DrawStatusDot(g, x + 2, y + 4, Red);
        g.DrawString("Memory Access", MonoFont, new SolidBrush(TextPrimary), x + 18, y);
        g.DrawString("offline", SmallFont, new SolidBrush(TextMuted), x + 130, y);
        y += 30;

        g.DrawString("Version & Updates", ItemFont, new SolidBrush(TextSecondary), x, y);
        y += 24;

        DrawLabel(g, x, y, w, "Build", UpdateService.VersionString);
        y += 22;

        int dotX = x + 2;
        Color dotCol = _updateStatus switch
        {
            "up to date" => Green,
            "update available" => Yellow,
            "check error" => Red,
            _ when _updateStatus.Contains("error") => Red,
            _ when _updateStatus.Contains("available") => Yellow,
            _ => TextMuted
        };
        DrawStatusDot(g, dotX, y + 4, dotCol);
        g.DrawString(_updateStatus, SmallFont, new SolidBrush(_updateStatusColor), x + 16, y + 1);
        y += 22;
    }

    private static void DrawToggleRow(Graphics g, int x, int y, int w, string label,
        bool value, Action<bool> setter, HudElement.StatusLevel status)
    {
        // Hover highlight
        var rowRect = new Rectangle(x - 4, y, w + 8, ItemHeight);
        bool hovered = IsHovered(rowRect);

        if (hovered)
        {
            using var hoverBrush = new SolidBrush(Color.FromArgb(20, 60, 160, 240));
            g.FillRectangle(hoverBrush, rowRect);
        }

        // Status dot
        Color dotColor = status switch
        {
            HudElement.StatusLevel.Active => Green,
            HudElement.StatusLevel.Warning => Yellow,
            HudElement.StatusLevel.Error => Red,
            _ => TextMuted
        };
        DrawStatusDot(g, x, y + 7, dotColor);

        // Label
        g.DrawString(label, ItemFont, new SolidBrush(hovered ? TextPrimary : TextSecondary), x + 16, y + 4);

        // Toggle switch
        int swX = x + w - 48;
        int swY = y + 5;
        int swW = 40;
        int swH = 18;

        using var swBg = new SolidBrush(value ? SwitchOn : SwitchOff);
        g.FillEllipse(swBg, swX, swY, swH, swH);
        g.FillRectangle(swBg, swX + swH / 2, swY, swW - swH, swH);
        g.FillEllipse(swBg, swX + swW - swH, swY, swH, swH);

        int knobX = value ? swX + swW - swH - 1 : swX + 1;
        using var knobBrush = new SolidBrush(Color.FromArgb(230, 230, 235));
        g.FillEllipse(knobBrush, knobX, swY + 1, swH - 2, swH - 2);

        // Click
        if (hovered && MouseTracker.LeftClicked)
        {
            var toggleRect = new Rectangle(swX, swY, swW, swH);
            if (toggleRect.Contains(MouseTracker.LastClickPoint))
                setter(!value);
        }
    }

    private static void DrawSlider(Graphics g, int x, int y, int w, float frac)
    {
        int trackW = w - 24;

        using var trackBrush = new SolidBrush(Color.FromArgb(50, 50, 58));
        g.FillRectangle(trackBrush, x + 12, y + 7, trackW, 4);

        int fillW = (int)(trackW * frac);
        using var fillBrush = new SolidBrush(Accent);
        g.FillRectangle(fillBrush, x + 12, y + 7, fillW, 4);

        int thumbX = x + 12 + fillW;
        using var thumbBrush = new SolidBrush(TextPrimary);
        g.FillEllipse(thumbBrush, thumbX - 4, y + 3, 10, 10);
    }

    private static void DrawLabel(Graphics g, int x, int y, int w, string label, string value)
    {
        g.DrawString(label, ItemFont, new SolidBrush(TextSecondary), x, y);
        g.DrawString(value, MonoFont, new SolidBrush(Accent), x + w - 60, y);
    }

    private static void DrawStatusDot(Graphics g, int x, int y, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x, y, 8, 8);
        using var glow = new SolidBrush(Color.FromArgb(60, color.R, color.G, color.B));
        g.FillEllipse(glow, x - 1, y - 1, 10, 10);
    }

    private static bool IsHovered(Rectangle rect)
    {
        return rect.Contains(MouseTracker.Position);
    }

    public void Dispose()
    {
        TitleFont.Dispose();
        TabFont.Dispose();
        ItemFont.Dispose();
        MonoFont.Dispose();
        SmallFont.Dispose();
    }
}
