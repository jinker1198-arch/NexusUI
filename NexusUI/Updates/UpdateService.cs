using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusUI.Updates;

internal sealed class UpdateService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private bool _disposed;

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public static string VersionString => CurrentVersion.ToString();

    public string UpdateUrl { get; set; }
    public string DownloadUrl { get; set; }

    public UpdateService(string updateUrl, string downloadUrl)
    {
        UpdateUrl = updateUrl;
        DownloadUrl = downloadUrl;
    }

    public async Task<(bool available, Version? latest, string? error)> CheckAsync()
    {
        try
        {
            using var resp = await _http.GetAsync(UpdateUrl).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            string body = (await resp.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();
            if (Version.TryParse(body, out var remote))
                return (remote > CurrentVersion, remote, null);
            return (false, null, $"Invalid version format: {body}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool ok, string? path, string? error)> DownloadAsync()
    {
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"NexusUI_{Guid.NewGuid():N}.exe");
            using var resp = await _http.GetAsync(DownloadUrl).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None);
            await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
            return (true, tmp, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public void ApplyUpdate(string downloadedPath)
    {
        string exePath = Application.ExecutablePath;
        string? dir = Path.GetDirectoryName(exePath);
        if (dir == null) return;

        string updater = Path.Combine(dir, "NexusUpdate.cmd");
        string exeName = Path.GetFileName(exePath);

        File.WriteAllText(updater,
            $"@echo off\r\n" +
            $":wait\r\n" +
            $"tasklist /FI \"IMAGENAME eq {exeName}\" 2>nul | find /I \"{exeName}\" >nul\r\n" +
            $"if %ERRORLEVEL% equ 0 (\r\n" +
            $"  timeout /t 1 /nobreak >nul\r\n" +
            $"  goto wait\r\n" +
            $")\r\n" +
            $"move /Y \"{downloadedPath}\" \"{exePath}\"\r\n" +
            $"start \"\" \"{exePath}\"\r\n" +
            $"del \"%~f0\"\r\n");

        Process.Start(new ProcessStartInfo(updater) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        Application.Exit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
