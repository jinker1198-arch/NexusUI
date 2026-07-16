namespace NexusUI.Integration;

internal static class Offsets
{
    public static nint ClientDll { get; set; }

    // Entity list
    public const int EntityList = 0x1A0AAC0;
    public const int EntityListEntrySize = 0x78;
    public const int EntityControllerOffset = 0x10;

    // Controller -> PlayerPawn
    public const int PawnHandle = 0x30C;

    // Pawn -> position/state
    public const int PosOrigin = 0x1324;
    public const int Health = 0x32C;
    public const int MaxHealth = 0x330;
    public const int TeamNum = 0x3BF;

    // Local player
    public const int LocalPlayerPawn = 0x17E0A18;

    // View matrix (client.dll + offset → flat 4x4 float array)
    public const int ViewMatrix = 0x1A3B3A0;

    // Bones — read from pawn via GameSceneNode
    public const int GameSceneNode = 0x310;           // C_BaseEntity::m_pGameSceneNode
    public const int ModelState = 0xD0;               // CGameSceneNode → m_modelState (CSkeletonInstance)
    public const int BoneArray = 0x30;                // CSkeletonInstance → m_boneArray (pointer to transforms)
    public const int BoneSize = 32;                   // bytes per bone (quat + vec)
    public const int BonePosOffset = 12;              // position starts at byte 12 within each bone entry

    // Key bone indices for skeleton drawing
    public const int BoneHead = 6;
    public const int BoneNeck = 5;
    public const int BoneSpine = 4;
    public const int BonePelvis = 2;
    public const int BoneLeftShoulder = 8;
    public const int BoneLeftElbow = 9;
    public const int BoneLeftHand = 10;
    public const int BoneRightShoulder = 11;
    public const int BoneRightElbow = 12;
    public const int BoneRightHand = 13;
    public const int BoneLeftKnee = 23;
    public const int BoneLeftFoot = 24;
    public const int BoneRightKnee = 26;
    public const int BoneRightFoot = 27;
}
