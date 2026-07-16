using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusUI.Updates;

internal sealed class RemoteControl : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _statusUrl;
    private bool _disposed;

    public RemoteControl(string statusUrl)
    {
        _statusUrl = statusUrl;
    }

    /// <summary>
    /// Checks the remote status endpoint.
    /// Returns (action, message, minVersion, downloadUrl).
    /// action: "ok" | "kill" | "update"
    /// </summary>
    public async Task<(string action, string message, string? minVersion, string? downloadUrl)> CheckAsync()
    {
        try
        {
            using var resp = await _http.GetAsync(_statusUrl).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            string body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();

            if (body.StartsWith("kill:", StringComparison.OrdinalIgnoreCase))
                return ("kill", body[5..].Trim(), null, null);

            if (body.StartsWith("update:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = body[7..].Split(':', 2);
                string minVer = parts.Length > 0 ? parts[0].Trim() : "0.0.0.0";
                string dlUrl = parts.Length > 1 ? parts[1].Trim() : "";
                return ("update", $"minimum version {minVer} required", minVer, dlUrl);
            }

            return ("ok", "", null, null);
        }
        catch
        {
            return ("ok", "", null, null);
        }
    }

    public void Kill(string message)
    {
        MessageBox.Show(message, "NexusUI — Shutdown", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        Environment.Exit(0);
    }

    public async Task ForceUpdateAsync(string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl)) return;

        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"NexusUI_{Guid.NewGuid():N}.exe");
        try
        {
            using var resp = await _http.GetAsync(downloadUrl).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var fs = new System.IO.FileStream(tmp, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
            await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        string exePath = Application.ExecutablePath;
        string? dir = System.IO.Path.GetDirectoryName(exePath);
        if (dir == null) return;

        string updater = System.IO.Path.Combine(dir, "NexusForceUpdate.cmd");
        string exeName = System.IO.Path.GetFileName(exePath);

        System.IO.File.WriteAllText(updater,
            $"@echo off\r\n" +
            $":wait\r\n" +
            $"tasklist /FI \"IMAGENAME eq {exeName}\" 2>nul | find /I \"{exeName}\" >nul\r\n" +
            $"if %ERRORLEVEL% equ 0 (\r\n" +
            $"  timeout /t 1 /nobreak >nul\r\n" +
            $"  goto wait\r\n" +
            $")\r\n" +
            $"move /Y \"{tmp}\" \"{exePath}\"\r\n" +
            $"start \"\" \"{exePath}\"\r\n" +
            $"del \"%~f0\"\r\n");

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(updater)
        {
            UseShellExecute = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        });
        Application.Exit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
