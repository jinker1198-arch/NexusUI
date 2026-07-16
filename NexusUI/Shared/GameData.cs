using System;
using System.Numerics;

namespace NexusUI.Shared;

public struct PlayerData
{
    public int Index;
    public int Health;
    public int MaxHealth;
    public int Team;
    public bool IsAlive;
    public bool IsLocalPlayer;
    public bool IsVisible;
    public Vector3 Position;
    public Vector2 ScreenPosition;
    public Vector2 ScreenHead;
    public Vector2 ScreenFeet;
    public Vector3[] Bones;
    public Vector2[] ScreenBones;
    public string Name;

    public static PlayerData Default => new()
    {
        Bones = Array.Empty<Vector3>(),
        ScreenBones = Array.Empty<Vector2>(),
        Name = ""
    };

    public readonly float ScreenHeight => Math.Abs(ScreenHead.Y - ScreenFeet.Y);
    public readonly float ScreenWidth => ScreenHeight * 0.4f;
    public readonly RectangleF ScreenBox
    {
        get
        {
            float h = ScreenHeight;
            float w = h * 0.4f;
            float cx = (ScreenFeet.X + ScreenHead.X) * 0.5f;
            return new RectangleF(cx - w * 0.5f, ScreenHead.Y, w, h);
        }
    }
}

public readonly struct RectangleF(float x, float y, float w, float h)
{
    public readonly float X = x, Y = y, Width = w, Height = h;
    public readonly float Left => X;
    public readonly float Top => Y;
    public readonly float Right => X + Width;
    public readonly float Bottom => Y + Height;
    public readonly float CenterX => X + Width * 0.5f;
}

public struct GameStateSnapshot
{
    public long Timestamp;
    public int PlayerCount;
    public PlayerData LocalPlayer;
    public PlayerData[] Players;
    public float[] ViewMatrix;
    public int MapId;
    public string MapName;
    public bool IsConnected;

    public static GameStateSnapshot Empty => new()
    {
        Timestamp = 0,
        PlayerCount = 0,
        Players = Array.Empty<PlayerData>(),
        ViewMatrix = Array.Empty<float>(),
        IsConnected = false,
        MapName = "disconnected"
    };
}
