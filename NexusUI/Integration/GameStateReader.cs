using System;
using System.Numerics;
using NexusUI.Shared;

namespace NexusUI.Integration;

internal sealed class GameStateReader
{
    private readonly MemoryReader _reader;
    private readonly GameProcess _process;
    private readonly ViewMatrix _viewMatrix = new();
    private nint _entityListPtr;
    private int _lastEntityListRefresh;
    private int _screenW, _screenH;

    private const int MaxPlayers = 64;
    private const int EntityListEntrySize = 0x78;
    private static readonly int[] KeyBones = {
        Offsets.BoneHead, Offsets.BoneNeck, Offsets.BoneSpine,
        Offsets.BonePelvis,
        Offsets.BoneLeftShoulder, Offsets.BoneLeftElbow, Offsets.BoneLeftHand,
        Offsets.BoneRightShoulder, Offsets.BoneRightElbow, Offsets.BoneRightHand,
        Offsets.BoneLeftKnee, Offsets.BoneLeftFoot,
        Offsets.BoneRightKnee, Offsets.BoneRightFoot
    };

    public GameStateReader(MemoryReader reader, GameProcess process)
    {
        _reader = reader;
        _process = process;
    }

    public void SetScreenSize(int w, int h) { _screenW = w; _screenH = h; }

    public GameStateSnapshot ReadState()
    {
        var state = new GameStateSnapshot
        {
            Timestamp = DateTime.UtcNow.Ticks,
            Players = new PlayerData[MaxPlayers],
            IsConnected = false,
            ViewMatrix = Array.Empty<float>()
        };

        if (!_process.IsValid || _screenW <= 0 || _screenH <= 0)
            return GameStateSnapshot.Empty;

        // View matrix
        if (!_viewMatrix.Read(_reader, _process.Handle, _process.ClientDllBase))
            return GameStateSnapshot.Empty;

        // Refresh entity list pointer
        if (_entityListPtr == 0 || ++_lastEntityListRefresh > 60)
        {
            _entityListPtr = _reader.Read<nint>(_process.Handle, _process.ClientDllBase + Offsets.EntityList);
            _lastEntityListRefresh = 0;
        }
        if (_entityListPtr == 0) return GameStateSnapshot.Empty;

        // Local player pawn
        nint localPawn = _reader.Read<nint>(_process.Handle, _process.ClientDllBase + Offsets.LocalPlayerPawn);
        if (localPawn == 0) return GameStateSnapshot.Empty;

        state.LocalPlayer = ReadPlayerData(localPawn, true, true);
        state.ViewMatrix = (float[])_viewMatrix.GetRaw().Clone();

        int playerIndex = 0;
        for (int i = 1; i <= MaxPlayers; i++)
        {
            nint entry = _entityListPtr + (i * EntityListEntrySize);
            nint controller = _reader.Read<nint>(_process.Handle, entry);
            if (controller == 0) continue;

            int pawnHandle = _reader.ReadInt32(_process.Handle, controller + Offsets.PawnHandle);
            if (pawnHandle <= 0) continue;

            int pawnIdx = pawnHandle & 0x7FFF;
            nint pawnEntry = _entityListPtr + (pawnIdx * EntityListEntrySize);
            nint pawn = _reader.Read<nint>(_process.Handle, pawnEntry + Offsets.EntityControllerOffset);
            if (pawn == 0 || pawn == localPawn) continue;

            var pd = ReadPlayerData(pawn, false, false);
            if (pd.Health > 0)
                state.Players[playerIndex++] = pd;
        }

        state.PlayerCount = playerIndex;
        state.IsConnected = true;
        state.MapName = "cs2";
        return state;
    }

    private PlayerData ReadPlayerData(nint pawn, bool isLocal, bool isLocalFull)
    {
        var pd = PlayerData.Default;
        pd.IsLocalPlayer = isLocal;

        pd.Position = new Vector3(
            _reader.ReadFloat(_process.Handle, pawn + Offsets.PosOrigin),
            _reader.ReadFloat(_process.Handle, pawn + Offsets.PosOrigin + 4),
            _reader.ReadFloat(_process.Handle, pawn + Offsets.PosOrigin + 8)
        );
        pd.Health = _reader.ReadInt32(_process.Handle, pawn + Offsets.Health);
        pd.MaxHealth = Math.Max(pd.Health, 100);
        pd.Team = _reader.ReadInt32(_process.Handle, pawn + Offsets.TeamNum);
        pd.Name = isLocal ? "local" : $"Player";

        // Bones
        nint sceneNode = _reader.Read<nint>(_process.Handle, pawn + Offsets.GameSceneNode);
        if (sceneNode != 0)
        {
            nint modelState = _reader.Read<nint>(_process.Handle, sceneNode + Offsets.ModelState);
            if (modelState != 0)
            {
                nint boneArray = _reader.Read<nint>(_process.Handle, modelState + Offsets.BoneArray);
                if (boneArray != 0)
                {
                    ReadBones(boneArray, ref pd);
                }
            }
        }

        // Screen project positions
        float[] vm = _viewMatrix.GetRaw();
        Vector3 headPos = pd.Bones.Length > 0 && pd.Bones[0] != default
            ? pd.Bones[0]
            : pd.Position + new Vector3(0, 0, 72);
        Vector3 feetPos = pd.Position - new Vector3(0, 0, 2);

        ViewMatrix.WorldToScreen(headPos, vm, _screenW, _screenH, out pd.ScreenHead);
        ViewMatrix.WorldToScreen(feetPos, vm, _screenW, _screenH, out pd.ScreenFeet);
        ViewMatrix.WorldToScreen(pd.Position, vm, _screenW, _screenH, out pd.ScreenPosition);

        // Project bones to screen
        for (int i = 0; i < pd.ScreenBones.Length; i++)
        {
            if (pd.Bones.Length > i)
                ViewMatrix.WorldToScreen(pd.Bones[i], vm, _screenW, _screenH, out pd.ScreenBones[i]);
        }

        pd.IsVisible = pd.ScreenFeet.Y > 0;
        return pd;
    }

    private void ReadBones(nint boneArray, ref PlayerData pd)
    {
        int count = KeyBones.Length;
        pd.Bones = new Vector3[count];
        pd.ScreenBones = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            nint addr = boneArray + (KeyBones[i] * Offsets.BoneSize) + Offsets.BonePosOffset;
            pd.Bones[i] = new Vector3(
                _reader.ReadFloat(_process.Handle, addr),
                _reader.ReadFloat(_process.Handle, addr + 4),
                _reader.ReadFloat(_process.Handle, addr + 8)
            );
        }
    }
}
