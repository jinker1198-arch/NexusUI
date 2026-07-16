using System;
using System.Numerics;

namespace NexusUI.Integration;

internal sealed class ViewMatrix
{
    private float[] _m = new float[16];
    private long _lastReadTick;

    public bool Read(MemoryReader reader, nint handle, nint clientDllBase)
    {
        long now = DateTime.UtcNow.Ticks;
        if (now - _lastReadTick < 330_000)
            return _m[0] != 0;

        nint addr = clientDllBase + Offsets.ViewMatrix;
        byte[] buf = new byte[64];

        if (!reader.ReadMemory(handle, addr, buf, 0, 64))
            return false;

        Buffer.BlockCopy(buf, 0, _m, 0, 64);
        _lastReadTick = now;
        return true;
    }

    public float[] GetRaw() => _m;

    public static bool WorldToScreen(Vector3 world, float[] viewMatrix, int screenW, int screenH, out Vector2 screen)
    {
        float w = viewMatrix[3] * world.X + viewMatrix[7] * world.Y + viewMatrix[11] * world.Z + viewMatrix[15];
        if (w < 0.001f)
        {
            screen = default;
            return false;
        }

        float x = viewMatrix[0] * world.X + viewMatrix[4] * world.Y + viewMatrix[8] * world.Z + viewMatrix[12];
        float y = viewMatrix[1] * world.X + viewMatrix[5] * world.Y + viewMatrix[9] * world.Z + viewMatrix[13];

        float invW = 1f / w;
        screen = new Vector2(
            screenW * 0.5f + (x * invW) * screenW * 0.5f,
            screenH * 0.5f - (y * invW) * screenH * 0.5f
        );
        return true;
    }

    public bool WorldToScreen(Vector3 world, int screenW, int screenH, out Vector2 screen)
        => WorldToScreen(world, _m, screenW, screenH, out screen);
}
