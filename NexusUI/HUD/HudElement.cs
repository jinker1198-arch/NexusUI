using System;
using System.Drawing;

namespace NexusUI.HUD;

public abstract class HudElement : IDisposable
{
    private bool _disposed;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public bool Visible { get; set; } = true;
    public Point Position { get; set; }
    public Size Size { get; set; }
    public int ZOrder { get; set; }
    public float Opacity { get; set; } = 1f;

    public enum StatusLevel { Inactive, Active, Warning, Error }
    public virtual StatusLevel Status { get; set; } = StatusLevel.Active;
    public virtual string StatusText { get; set; } = "ready";

    public abstract void Render(Graphics g, Rectangle viewport);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed) { _disposed = true; }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
