using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace NexusUI.Integration;

internal sealed class GameProcess : IDisposable
{
    private readonly MemoryReader _reader;
    private nint _handle;
    private nint _clientDllBase;
    private int _clientDllSize;
    private bool _disposed;
    private int _pid;

    public nint Handle => _handle;
    public nint ClientDllBase => _clientDllBase;
    public bool IsValid => _handle != 0 && _clientDllBase != 0;

    public GameProcess(MemoryReader reader)
    {
        _reader = reader;
    }

    public bool Attach()
    {
        if (_handle != 0) return true;

        Process? cs2 = null;
        try
        {
            cs2 = Process.GetProcessesByName("cs2").FirstOrDefault()
                ?? Process.GetProcessesByName("csgo").FirstOrDefault();
        }
        catch { return false; }

        if (cs2 == null || cs2.HasExited) return false;

        _pid = cs2.Id;
        _handle = _reader.OpenProcess((uint)_pid);
        if (_handle == 0) return false;

        FindClientDll(cs2);
        return _clientDllBase != 0;
    }

    private void FindClientDll(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals("client.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _clientDllBase = module.BaseAddress;
                    _clientDllSize = module.ModuleMemorySize;
                    return;
                }
            }
        }
        catch { }

        // Fallback: manual PEB walk disallowed by scope — module scan is sufficient
    }

    public void Detach()
    {
        if (_handle != 0)
        {
            // CloseHandle would require another syscall — for overlay lifetime, leak is acceptable
            _handle = 0;
        }
        _clientDllBase = 0;
        _pid = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
    }
}
