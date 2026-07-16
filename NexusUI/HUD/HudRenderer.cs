using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NexusUI.HUD;

internal sealed class HudRenderer : IDisposable
{
    private readonly List<HudElement> _elements = new();
    private readonly ControlPanel _panel;
    private bool _disposed;

    public bool MenuOpen
    {
        get => _panel.Visible;
        set => _panel.Visible = value;
    }

    public HudRenderer()
    {
        _panel = new ControlPanel(this);
    }

    public T Add<T>(T element) where T : HudElement
    {
        _elements.Add(element);
        return element;
    }

    public void Remove(HudElement element) => _elements.Remove(element);
    public IReadOnlyList<HudElement> Elements => _elements;
    public ControlPanel Panel => _panel;

    public void HandleKey(Keys key)
    {
    }

    public void Render(Graphics g, Rectangle bounds)
    {
        _panel.Render(g, bounds);

        var visible = _elements
            .Where(e => e.Visible)
            .OrderBy(e => e.ZOrder)
            .ToList();

        foreach (var element in visible)
            element.Render(g, bounds);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _panel.Dispose();
        foreach (var e in _elements) e.Dispose();
        _elements.Clear();
    }
}
