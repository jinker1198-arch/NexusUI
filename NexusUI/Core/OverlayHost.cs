using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusUI.HUD;

namespace NexusUI.Core;

internal sealed class OverlayHost : IDisposable
{
    private readonly Form _window;
    private bool _disposed;
    private readonly BufferedGraphicsContext _bufferContext;
    private BufferedGraphics? _buffer;
    private int _windowBaseExStyle;

    public event Action<Graphics, Rectangle>? RenderOverlay;
    public event Action<Keys>? KeyPressed;

    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public OverlayHost()
    {
        _bufferContext = BufferedGraphicsManager.Current;

        _window = new Form
        {
            Text = "NexusUI",
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.Black,
            TransparencyKey = Color.Black,
            StartPosition = FormStartPosition.Manual,
            Bounds = Screen.PrimaryScreen!.Bounds,
            MinimizeBox = false,
            MaximizeBox = false,
            ControlBox = false
        };

        _window.Load += OnLoad;
        _window.Paint += OnPaint;
        _window.Resize += OnResize;
        _window.KeyDown += OnKeyDown;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        int exStyle = GetWindowLong(_window.Handle, GWL_EXSTYLE);
        _windowBaseExStyle = exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
        SetWindowLong(_window.Handle, GWL_EXSTYLE, _windowBaseExStyle);

        SetWindowPos(_window.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        MouseTracker.Attach(_window);
    }

    public void SetClickThrough(bool clickThrough)
    {
        int ex = _windowBaseExStyle;
        if (clickThrough)
            ex |= WS_EX_TRANSPARENT;
        else
            ex &= ~WS_EX_TRANSPARENT;
        SetWindowLong(_window.Handle, GWL_EXSTYLE, ex);
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        if (_window.Width <= 0 || _window.Height <= 0) return;

        _buffer?.Dispose();
        _buffer = _bufferContext.Allocate(e.Graphics, new Rectangle(0, 0, _window.Width, _window.Height));

        using var g = _buffer.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        RenderOverlay?.Invoke(g, new Rectangle(0, 0, _window.Width, _window.Height));

        _buffer.Render(e.Graphics);

        MouseTracker.EndFrame();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        _window.Invalidate();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        KeyPressed?.Invoke(e.KeyCode);
    }

    public Form GetForm() => _window;

    public void InvalidateOverlay() => _window.Invalidate();

    public void Show()
    {
        _window.Show();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _buffer?.Dispose();
        _bufferContext?.Dispose();
        _window?.Dispose();
    }
}
