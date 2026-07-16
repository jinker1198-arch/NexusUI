using System;
using System.Diagnostics;
using System.Drawing;

namespace NexusUI.HUD;

public class FpsCounter : TextLabel
{
    private readonly Stopwatch _timer = Stopwatch.StartNew();
    private int _frameCount;
    private double _lastUpdate;

    public FpsCounter()
    {
        Text = "FPS: --";
        TextColor = Color.FromArgb(60, 200, 240);
        FontSize = 11f;
        FontStyle = FontStyle.Bold;
        Id = "__fps";
    }

    public override void Render(Graphics g, Rectangle viewport)
    {
        _frameCount++;
        double elapsed = _timer.Elapsed.TotalSeconds;
        if (elapsed - _lastUpdate >= 0.5)
        {
            double fps = _frameCount / (elapsed - _lastUpdate);
            _frameCount = 0;
            _lastUpdate = elapsed;
            Text = $"FPS: {fps:F0}";
        }

        base.Render(g, viewport);
    }
}
