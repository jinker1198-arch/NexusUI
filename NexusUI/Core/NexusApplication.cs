using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using NexusUI.HUD;
using NexusUI.Integration;
using NexusUI.Shared;
using NexusUI.Updates;

namespace NexusUI.Core;

internal sealed class NexusApplication : IDisposable
{
    private readonly OverlayHost _host;
    private readonly HudRenderer _renderer;
    private bool _disposed;
    private readonly Timer _renderTimer;
    private readonly Timer _gameStateTimer;
    private readonly Timer _remoteTimer;

    private readonly MemoryReader _memoryReader;
    private readonly GameProcess _gameProcess;
    private readonly GameStateReader _stateReader;
    private readonly UpdateService _updater;
    private readonly RemoteControl _remote;

    private readonly HudPanel _infoPanel;
    private readonly TextLabel _statusLabel;
    private readonly Crosshair _crosshair;
    private readonly FpsCounter _fps;
    private readonly Radar _radar;
    private readonly PlayerEsp _esp;

    public NexusApplication()
    {
        _host = new OverlayHost();
        _renderer = new HudRenderer();
        _host.KeyPressed += OnKeyPressed;
        _host.RenderOverlay += _renderer.Render;

        // Memory reader chain
        _memoryReader = new MemoryReader();
        _gameProcess = new GameProcess(_memoryReader);
        _stateReader = new GameStateReader(_memoryReader, _gameProcess);

        // Info panel
        _infoPanel = new HudPanel
        {
            Position = new Point(10, 6),
            Size = new Size(200, 130),
            ZOrder = 0,
            Title = "NexusUI",
            BackgroundColor = Color.FromArgb(160, 10, 10, 14),
            BorderColor = Color.FromArgb(60, 160, 240)
        };
        _renderer.Add(_infoPanel);

        _statusLabel = new TextLabel
        {
            Text = "Scanning for CS2...",
            Position = new Point(24, 32),
            Size = new Size(160, 20),
            TextColor = Color.FromArgb(200, 200, 80),
            FontSize = 10,
            FontStyle = FontStyle.Bold,
            ZOrder = 1
        };
        _renderer.Add(_statusLabel);

        _renderer.Add(new HUD.ProgressBar
        {
            Position = new Point(24, 56),
            Size = new Size(172, 14),
            Value = 100,
            FillColor = Color.FromArgb(60, 200, 120),
            Label = "Health",
            ZOrder = 1
        });

        _renderer.Add(new HUD.ProgressBar
        {
            Position = new Point(24, 74),
            Size = new Size(172, 14),
            Value = 100,
            FillColor = Color.FromArgb(60, 160, 240),
            Label = "Armor",
            ZOrder = 1
        });

        _crosshair = new Crosshair
        {
            CrosshairStyle = Crosshair.Style.Cross,
            Gap = 3,
            Length = 10,
            Thickness = 2f,
            CrosshairColor = Color.FromArgb(80, 180, 255),
            ZOrder = 100,
            Status = HudElement.StatusLevel.Active,
            StatusText = "ready"
        };
        _renderer.Add(_crosshair);

        _fps = new FpsCounter
        {
            Position = new Point(12, 8),
            Size = new Size(120, 22),
            ZOrder = 200
        };
        _renderer.Add(_fps);

        // Radar
        _radar = new Radar
        {
            Position = new Point(12, 140),
            Radius = 120,
            WorldScale = 0.035f,
            ZOrder = 50,
            Status = HudElement.StatusLevel.Inactive,
            StatusText = "awaiting game"
        };
        _renderer.Add(_radar);

        // Player ESP (boxes, skeleton, health, name)
        _esp = new PlayerEsp
        {
            ZOrder = 90,
            Visible = true,
            Status = HudElement.StatusLevel.Inactive,
            StatusText = "offscreen"
        };
        _renderer.Add(_esp);

        // Update service
        _updater = new UpdateService(
            "https://raw.githubusercontent.com/YOUR_USER/YOUR_REPO/main/version.txt",
            "https://github.com/YOUR_USER/YOUR_REPO/releases/latest/download/NexusUI.exe");

        // Timers
        _renderTimer = new Timer { Interval = 16 };
        _renderTimer.Tick += RenderFrame;

        _gameStateTimer = new Timer { Interval = 33 };
        _gameStateTimer.Tick += PollGameState;

        // Remote kill-switch + forced update (polls every 60s)
        _remote = new RemoteControl(
            "https://raw.githubusercontent.com/YOUR_USER/YOUR_REPO/main/status.txt");
        _remoteTimer = new Timer { Interval = 60_000 };
        _remoteTimer.Tick += PollRemoteAsync;

        _ = CheckForUpdatesAsync();
    }

    public void Run()
    {
        _host.Show();
        _renderTimer.Start();
        _gameStateTimer.Start();
        _remoteTimer.Start();
        Application.Run(_host.GetForm());
    }

    private void OnKeyPressed(Keys key)
    {
        if (key == Keys.Insert)
        {
            _renderer.MenuOpen = !_renderer.MenuOpen;
            _host.SetClickThrough(!_renderer.MenuOpen);
            return;
        }
        if (key == Keys.End)
            _host.GetForm().Close();
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        _host.InvalidateOverlay();
    }

    private void PollGameState(object? sender, EventArgs e)
    {
        var form = _host.GetForm();
        _stateReader.SetScreenSize(form.ClientSize.Width, form.ClientSize.Height);

        if (!_gameProcess.IsValid)
        {
            if (!_gameProcess.Attach())
            {
                _radar.Visible = false;
                _esp.Visible = false;
                _statusLabel.Text = "Waiting for CS2.exe...";
                _statusLabel.TextColor = Color.FromArgb(200, 200, 80);
                return;
            }
            _statusLabel.Text = "Connected to CS2";
            _statusLabel.TextColor = Color.FromArgb(80, 220, 130);
            _radar.Visible = true;
            _esp.Visible = true;
        }

        var state = _stateReader.ReadState();

        if (state.IsConnected)
        {
            _radar.UpdateState(state);
            _esp.UpdateState(state);

            _radar.Status = HudElement.StatusLevel.Active;
            _radar.StatusText = $"tracking {state.PlayerCount} players";
            _esp.Status = state.PlayerCount > 0
                ? HudElement.StatusLevel.Active
                : HudElement.StatusLevel.Warning;
            _esp.StatusText = state.PlayerCount > 0
                ? $"rendering {state.PlayerCount} players"
                : "no players";

            if (state.LocalPlayer.Health > 0)
            {
                _radar.Visible = true;
                _crosshair.CrosshairColor = Color.FromArgb(80, 180, 255);
            }
            else
            {
                _crosshair.CrosshairColor = Color.FromArgb(160, 80, 80);
            }
        }
        else
        {
            _radar.Status = HudElement.StatusLevel.Warning;
            _radar.StatusText = "no state data";
            _esp.Status = HudElement.StatusLevel.Warning;
            _esp.StatusText = "no state data";
        }
    }

    private async void PollRemoteAsync(object? sender, EventArgs e)
    {
        var (action, msg, minVer, dlUrl) = await _remote.CheckAsync().ConfigureAwait(true);

        switch (action)
        {
            case "kill":
                _remote.Kill(msg);
                break;

            case "update":
                var current = UpdateService.CurrentVersion;
                if (minVer != null && Version.TryParse(minVer, out var min) && current < min)
                {
                    _renderer.Panel.SetUpdateStatus("force-updating...", Color.FromArgb(230, 190, 50));
                    await _remote.ForceUpdateAsync(dlUrl ?? _updater.DownloadUrl).ConfigureAwait(true);
                }
                break;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        var (available, latest, error) = await _updater.CheckAsync().ConfigureAwait(true);

        if (error != null)
        {
            _renderer.Panel.SetUpdateStatus($"check error: {error}", Color.FromArgb(230, 90, 90));
            return;
        }

        _renderer.Panel.SetUpdateStatus(
            available
                ? $"update {latest} available — restart to apply"
                : $"up to date ({UpdateService.VersionString})",
            available
                ? Color.FromArgb(230, 190, 50)
                : Color.FromArgb(120, 120, 130));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderTimer.Stop();
        _renderTimer.Dispose();
        _gameStateTimer.Stop();
        _gameStateTimer.Dispose();
        _remoteTimer.Stop();
        _remoteTimer.Dispose();
        _host.Dispose();
        _renderer.Dispose();
        _memoryReader.Dispose();
        _gameProcess.Dispose();
        _updater.Dispose();
        _remote.Dispose();
    }
}
