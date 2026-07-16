using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusUI.Integration;

internal sealed class MemoryReader : IDisposable
{
    private readonly NtOpenProcessSyscall _ntOpenProcess;
    private readonly NtReadVirtualMemorySyscall _ntReadVirtualMemory;
    private readonly List<nint> _allocatedStubs = new();
    private bool _disposed;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtOpenProcessSyscall(nint handlePtr, nint desiredAccess, nint objAttr, nint clientId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtReadVirtualMemorySyscall(nint handle, nint baseAddress, nint buffer, nint size, out nint bytesRead);

    public MemoryReader()
    {
        int openSsn = ExtractSyscallNumber("NtOpenProcess");
        int readSsn = ExtractSyscallNumber("NtReadVirtualMemory");

        _ntOpenProcess = CreateStub<NtOpenProcessSyscall>(openSsn);
        _ntReadVirtualMemory = CreateStub<NtReadVirtualMemorySyscall>(readSsn);
    }

    private static int ExtractSyscallNumber(string apiName)
    {
        nint ntdll = NativeLibrary.Load("ntdll.dll");
        nint funcAddr = NativeLibrary.GetExport(ntdll, apiName);
        byte[] code = new byte[20];
        Marshal.Copy(funcAddr, code, 0, 20);

        for (int i = 0; i < 12; i++)
            if (code[i] == 0xB8 && i + 6 < 20 && code[i + 5] == 0x0F && code[i + 6] == 0x05)
                return code[i + 1] | (code[i + 2] << 8) | (code[i + 3] << 16) | (code[i + 4] << 24);

        return 0;
    }

    private T CreateStub<T>(int syscallNum) where T : class
    {
        byte[] stub =
        [
            0x4C, 0x8B, 0xD1,                                      // mov r10, rcx
            0xB8, (byte)(syscallNum & 0xFF), (byte)((syscallNum >> 8) & 0xFF),
                  (byte)((syscallNum >> 16) & 0xFF), (byte)((syscallNum >> 24) & 0xFF), // mov eax, #
            0x0F, 0x05,                                             // syscall
            0xC3                                                    // ret
        ];

        nint mem = VirtualAlloc(IntPtr.Zero, (uint)stub.Length, MemCommit | MemReserve, PageExecuteReadwrite);
        Marshal.Copy(stub, 0, mem, stub.Length);
        _allocatedStubs.Add(mem);
        return Marshal.GetDelegateForFunctionPointer<T>(mem);
    }

    public nint OpenProcess(uint pid)
    {
        nint handle = 0;
        var cid = new CLIENT_ID { UniqueProcess = (nint)pid };
        var oa = new OBJECT_ATTRIBUTES { Length = (uint)Marshal.SizeOf<OBJECT_ATTRIBUTES>() };

        nint oaPtr = Marshal.AllocHGlobal(Marshal.SizeOf<OBJECT_ATTRIBUTES>());
        nint cidPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CLIENT_ID>());
        nint handlePtr = Marshal.AllocHGlobal(nint.Size);
        try
        {
            Marshal.StructureToPtr(oa, oaPtr, false);
            Marshal.StructureToPtr(cid, cidPtr, false);

            int status = _ntOpenProcess(handlePtr, 0x0410, oaPtr, cidPtr);
            if (status >= 0)
                handle = Marshal.ReadIntPtr(handlePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(handlePtr);
            Marshal.FreeHGlobal(cidPtr);
            Marshal.FreeHGlobal(oaPtr);
        }
        return handle;
    }

    public bool ReadMemory(nint handle, nint address, byte[] buffer, int offset, int count)
    {
        nint localPtr = Marshal.AllocHGlobal(count);
        try
        {
            int status = _ntReadVirtualMemory(handle, address + offset, localPtr, count, out _);
            if (status >= 0)
            {
                Marshal.Copy(localPtr, buffer, 0, count);
                return true;
            }
            return false;
        }
        finally { Marshal.FreeHGlobal(localPtr); }
    }

    public T Read<T>(nint handle, nint address, int offset = 0) where T : unmanaged
    {
        byte[] buf = new byte[Marshal.SizeOf<T>()];
        return ReadMemory(handle, address, buf, offset, buf.Length)
            ? MemoryMarshal.Read<T>(buf)
            : default;
    }

    public int ReadInt32(nint handle, nint address, int offset = 0)
    {
        byte[] buf = new byte[4];
        return ReadMemory(handle, address, buf, offset, 4) ? BitConverter.ToInt32(buf) : 0;
    }

    public float ReadFloat(nint handle, nint address, int offset = 0)
    {
        byte[] buf = new byte[4];
        return ReadMemory(handle, address, buf, offset, 4) ? BitConverter.ToSingle(buf) : 0f;
    }

    [DllImport("kernel32", SetLastError = true)]
    private static extern nint VirtualAlloc(nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool VirtualFree(nint lpAddress, uint dwSize, uint dwFreeType);

    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint PageExecuteReadwrite = 0x40;

    [StructLayout(LayoutKind.Sequential)]
    private struct CLIENT_ID { public nint UniqueProcess; public nint UniqueThread; }

    [StructLayout(LayoutKind.Sequential)]
    private struct OBJECT_ATTRIBUTES
    {
        public uint Length;
        public nint RootDirectory;
        public nint ObjectName;
        public uint Attributes;
        public nint SecurityDescriptor;
        public nint SecurityQualityOfService;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (nint stub in _allocatedStubs)
            VirtualFree(stub, 0, 0x8000);
        _allocatedStubs.Clear();
    }
}
