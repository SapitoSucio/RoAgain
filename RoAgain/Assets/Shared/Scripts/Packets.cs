using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

abstract public class Packet
{
    public int SessionId;
    public abstract int PacketType { get; }

    public virtual bool Validate() { return true; }

    public byte[] SerializeJson()
    {
        if (!Validate())
        {
            OwlLogger.LogError($"Packet of type {GetType().Name} failed to Validate for serialization!", GameComponent.Network);
            return null;
        }

        string data = PacketType.ToString() + "|";
        string packetBody = JsonUtility.ToJson(this);
        if(packetBody.Contains('|'))
        {
            OwlLogger.LogError($"Tried to send packet that contains special character '|', not allowed!", GameComponent.Network);
            return null;
        }

        data += packetBody;
        data += "|";
        return System.Text.Encoding.UTF8.GetBytes(data);
    }

    public static string[] SplitIntoPackets(byte[] data, out byte[] remainder)
    {
        string strData = System.Text.Encoding.UTF8.GetString(data);
        string[] strPieces = strData.Split("|", StringSplitOptions.RemoveEmptyEntries);

        // rudimentary check that we _could_ be looking at a properly aligned start of a packet
        int i = 0;
        if (!int.TryParse(strPieces[0], out int candidatePacketType))
        {
            // First segment can't be a packetType - misaligned!
            // We skip the first segment & hope that fixes it.
            // over time, this will discard any segment that isn't a packet type
            i = 1;
        }

        List<string> packets = new();
        for(; i < strPieces.Length -1; i+=2)
        {
            string packetData = strPieces[i] + "|" + strPieces[i+1];
            packets.Add(packetData);
        }

        if(i < strPieces.Length -1)
        {
            // There is a segment left-over at the end that didn't fit into the packets
            // Can't be more than 1 segment since the above for-loop iterates on at most 2 elements at once
            remainder = System.Text.Encoding.UTF8.GetBytes(strPieces[i ^ 1]);
        }
        else
        {
            remainder = new byte[0];
        }

        return packets.ToArray();
    }

    public static Packet DeserializeJson(string data)
    {
        string[] strPieces = data.Split('|');
        Packet packet;

        int packetType = int.Parse(strPieces[0]);
        string json = strPieces[1];

        // TODO: replace this with a dynamically generated lookup
        switch (packetType)
        {
            case 1:
                packet = JsonUtility.FromJson<LoginRequestPacket>(json);
                break;
            case 2:
                packet = JsonUtility.FromJson<LoginResponsePacket>(json);
                break;
            case 5:
                packet = JsonUtility.FromJson<CharacterSelectionRequestPacket>(json);
                break;
            case 6:
                packet = JsonUtility.FromJson<CharacterSelectionDataPacket>(json);
                break;
            case 7:
                packet = JsonUtility.FromJson<CharacterLoginPacket>(json);
                break;
            //case 8:
            //    packet = JsonUtility.FromJson<CharacterRuntimeDataPacket>(json);
            //    break;
            //case 9:
            //    packet = JsonUtility.FromJson<CharacterLoginCompletedPacket>(json);
            //    break;
            case 10:
                packet = JsonUtility.FromJson<MovementRequestPacket>(json);
                break;
            case 11:
                packet = JsonUtility.FromJson<EntityPathUpdatePacket>(json);
                break;
            //case 12:
            //    packet = JsonUtility.FromJson<EntityDataPacket>(json);
            //    break;
            case 13:
                packet = JsonUtility.FromJson<SessionCreationPacket>(json);
                break;
            case 14:
                packet = JsonUtility.FromJson<EntityRemovedPacket>(json);
                break;
            //case 15:
            //    packet = JsonUtility.FromJson<MapChangePacket>(json);
            //    break;
            case 16:
                packet = JsonUtility.FromJson<CellEffectGroupPlacedPacket>(json);
                break;
            case 17:
                packet = JsonUtility.FromJson<CellEffectGroupRemovedPacket>(json);
                break;
            case 18:
                packet = JsonUtility.FromJson<SkillUseEntityRequestPacket>(json);
                break;
            case 19:
                packet = JsonUtility.FromJson<SkillUseGroundRequestPacket>(json);
                break;
            case 20:
                packet = JsonUtility.FromJson<DamageTakenPacket>(json);
                break;
            case 21:
                packet = JsonUtility.FromJson<ChatMessagePacket>(json);
                break;
            case 22:
                packet = JsonUtility.FromJson<CastProgressPacket>(json);
                break;
            case 23:
                packet = JsonUtility.FromJson<EntitySkillExecutePacket>(json);
                break;
            case 24:
                packet = JsonUtility.FromJson<GroundSkillExecutePacket>(json);
                break;
            case 25:
                packet = JsonUtility.FromJson<HpUpdatePacket>(json);
                break;
            case 26:
                packet = JsonUtility.FromJson<SpUpdatePacket>(json);
                break;
            case 27:
                packet = JsonUtility.FromJson<SkillFailPacket>(json);
                break;
            case 28:
                packet = JsonUtility.FromJson<ChatMessageRequestPacket>(json);
                break;
            case 29:
                packet = JsonUtility.FromJson<GridEntityDataPacket>(json);
                break;
            case 30:
                packet = JsonUtility.FromJson<BattleEntityDataPacket>(json);
                break;
            case 31:
                packet = JsonUtility.FromJson<RemoteCharacterDataPacket>(json);
                break;
            case 32:
                packet = JsonUtility.FromJson<LocalCharacterDataPacket>(json);
                break;
            case 33:
                packet = JsonUtility.FromJson<ExpUpdatePacket>(json);
                break;
            case 34:
                packet = JsonUtility.FromJson<ItemReceivedPacket>(json);
                break;
            case 35:
                packet = JsonUtility.FromJson<EquipChangedPacket>(json);
                break;
            case 36:
                packet = JsonUtility.FromJson<PlayerEquipmentInfoPacket>(json);
                break;
            case 37:
                packet = JsonUtility.FromJson<StatUpdatePacket>(json);
                break;
            case 38:
                packet = JsonUtility.FromJson<InventoryStatePacket>(json);
                break;
            case 39:
                packet = JsonUtility.FromJson<StatIncreaseRequestPacket>(json);
                break;
            case 40:
                packet = JsonUtility.FromJson<StatFloatUpdatePacket>(json);
                break;
            case 41:
                packet = JsonUtility.FromJson<StatCostUpdatePacket>(json);
                break;
            case 42:
                packet = JsonUtility.FromJson<StatPointUpdatePacket>(json);
                break;
            case 43:
                packet = JsonUtility.FromJson<LevelUpPacket>(json);
                break;
            case 44:
                packet = JsonUtility.FromJson<AccountCreationRequestPacket>(json);
                break;
            case 45:
                packet = JsonUtility.FromJson<AccountCreationResponsePacket>(json);
                break;
            case 46:
                packet = JsonUtility.FromJson<CharacterCreationRequestPacket>(json);
                break;
            case 47:
                packet = JsonUtility.FromJson<CharacterCreationResponsePacket>(json);
                break;
            case 48:
                packet = JsonUtility.FromJson<AccountDeletionRequestPacket>(json);
                break;
            case 49:
                packet = JsonUtility.FromJson<AccountDeletionResponsePacket>(json);
                break;
            case 50:
                packet = JsonUtility.FromJson<CharacterDeletionRequestPacket>(json);
                break;
            case 51:
                packet = JsonUtility.FromJson<CharacterDeletionResponsePacket>(json);
                break;
            case 52:
                packet = JsonUtility.FromJson<LocalPlayerEntitySkillQueuedPacket>(json);
                break;
            case 53:
                packet = JsonUtility.FromJson<LocalPlayerGroundSkillQueuedPacket>(json);
                break;
            case 54:
                packet = JsonUtility.FromJson<SkillTreeEntryPacket>(json);
                break;
            case 55:
                packet = JsonUtility.FromJson<SkillTreeRemovePacket>(json);
                break;
            case 56:
                packet = JsonUtility.FromJson<SkillPointAllocateRequestPacket>(json);
                break;
            case 57:
                packet = JsonUtility.FromJson<SkillPointUpdatePacket>(json);
                break;
            case 58:
                packet = JsonUtility.FromJson<ReturnAfterDeathRequestPacket>(json);
                break;
            default:
                OwlLogger.LogError($"Invalid Packet type {packetType} received!", GameComponent.Network);
                return null;
        }

        if(!packet.Validate())
        {
            OwlLogger.LogError($"Packet type {packet.GetType().Name} failed validation after Deserialize!", GameComponent.Network);
            return null;
        }

        return packet;
    }
}

// TODO: Set strings to fixed length so we can wait for bytes accurately without relying on delimiter-characters
public class LoginRequestPacket : Packet
{
    public override int PacketType => 1;

    public string Username;
    public string Password;
}

public class LoginResponsePacket : Packet
{
    public override int PacketType => 2;

    public bool IsSuccessful;

    public LoginResponsePacket(bool isSuccessful)
    {
        IsSuccessful = isSuccessful;
    }
}

public class LoginResponse
{ 
    public bool IsSuccessful;
    public int SessionId;
}

public class CharacterSelectionRequestPacket : Packet
{
    public override int PacketType => 5;
}

public class CharacterSelectionDataPacket : Packet
{
    public override int PacketType => 6;

    public CharacterSelectionData Data;
    public int Count;
    public int Index;
}

// TODO: Set strings to fixed length so we can wait for bytes accurately without relying on delimiter-characters
[Serializable]
public class CharacterSelectionData
{
    public int CharacterId;
    public string Name;
    public string MapId;
    public int BaseLevel;
    public JobId JobId;
    public int JobLevel;
    public int BaseExp;
    public int Hp;
    public int Sp;
    public int Str;
    public int Agi;
    public int Vit;
    public int Int;
    public int Dex;
    public int Luk;
    // TODO: Add stats that are shown on char selection
    public object VisualsOrGear; // TODO
}

public class CharacterLoginPacket : Packet
{
    public override int PacketType => 7;

    public int CharacterId;
}


public class MovementRequestPacket : Packet
{
    public override int PacketType => 10;

    public Vector2Int TargetCoordinates;
}

public class EntityPathUpdatePacket : Packet
{
    public override int PacketType => 11;

    public int UnitId;
    public GridData.Path Path;
}

public enum EntityType
{
    Unknown,
    GenericGrid,
    GenericBattle,
    RemoteCharacter,
    LocalCharacter,
}

public class SessionCreationPacket : Packet
{
    public override int PacketType => 13;

    public int AssignedSessionId;
}

public class EntityRemovedPacket : Packet
{
    public override int PacketType => 14;

    public int EntityId;
}

public class CellEffectGroupPlacedPacket : Packet
{
    public override int PacketType => 16;

    public int GroupId;
    public CellEffectType Type;
    [SerializeReference]
    public GridShape Shape;
}

public class CellEffectGroupRemovedPacket : Packet
{
    public override int PacketType => 17;

    public int GroupId;
}

public class SkillUseEntityRequestPacket : Packet
{
    public override int PacketType => 18;

    public SkillId SkillId;
    public int SkillLvl;
    public int TargetId;
    //public int[] Params; // Not sure if there's enough cases where the Client sets parameters
    
}

public class SkillUseGroundRequestPacket : Packet
{
    public override int PacketType => 19;

    public SkillId SkillId;
    public int SkillLvl;
    public Vector2Int TargetCoords;
    //public int[] Params; // Not sure if there's enough cases where the Client sets parameters
}

public class CastProgressPacket : Packet
{
    public override int PacketType => 22;

    public int CasterId;
    public SkillId SkillId;
    public int TargetId;
    public Vector2Int TargetCoords;
    public float CastTimeTotal;
    public float CastTimeRemaining;
}

// TODO: CastInterruptPacket? Or make that a special configuration of CastProgressPacket?

// Should this packet exist? Or do we instead want to have a "vfx"-packet that can be re-used for other vfx
// and let Packets for Damage, Status-Apply, CellGroupPlacement etc. tell the Client all that's necessary?
// Big question: Does the CastProgressPacket & EntityDataPacket contain all necessary info to play the execute-animation correctly (Animation-delay, etc)
public class EntitySkillExecutePacket : Packet
{
    public override int PacketType => 23;

    public SkillId SkillId;
    // SkillLevel needed?
    public int UserId;
    public int TargetId;
    public float AnimCd;
    // Untyped params?
    public bool Speak;
}

public class GroundSkillExecutePacket : Packet
{
    public override int PacketType => 24;

    public SkillId SkillId;
    // SkillLevel needed?
    public int UserId;
    public Vector2Int TargetCoords;
    public float AnimCd;
    // Untyped Params?
    public bool Speak;
}

// TODO: Skill Cooldown packet

// Serialization/verification code of this will use fixed length for any string field so that packet length is known at compile time (for waiting on bytes)
public class ChatMessagePacket : Packet
{
    public override int PacketType => 21;

    // This cannot easily accomodate named chat channels (since an additional string to the SenderName is needed)
    // TODO: Named channels/scopes
    public enum Scope
    {
        Global = 1,
        Whisper = 2,
        Proximity = 3,
        Party = 4,
        Guild = 5,
        Announce = 6,
    }

    public int SenderId;
    public string SenderName;
    public string Message;
    public Scope MessageScope;
}

public class ChatMessageRequestPacket : Packet
{
    public override int PacketType => 28;

    public const int NAME_LENGTH = 20;
    public const int MESSAGE_LENGTH = 200;
    public const string TARGET_GLOBAL = "tar_global"; // padding will be removed before comparing
    public const string TARGET_PROX = "tar_prox"; // padding will be removed before comparing

    public int SenderId;
    public string Message;
    public string TargetName;

    public override bool Validate()
    {
        if (!base.Validate())
            return false;

        if (TargetName.Length != NAME_LENGTH)
            return false;

        if (Message.Length != MESSAGE_LENGTH)
            return false;

        return true;
    }
}

// Should this packet also contain new HP/SP value? or do we rely on the client applying the difference itself?
public class DamageTakenPacket : Packet
{
    public override int PacketType => 20;

    public int EntityId;
    public int Damage; // For now: -1 = miss, -2 perfect dodge
    public bool IsSpDamage;
    // TODO: Additional display params: Crit, Chain
}

public class HpUpdatePacket : Packet
{
    public override int PacketType => 25;

    public int EntityId;
    public int NewHp;
}

public class SpUpdatePacket : Packet
{
    public override int PacketType => 26;

    public int EntityId;
    public int NewSp;
}

public class SkillFailPacket : Packet
{
    public override int PacketType => 27;

    public int EntityId;
    public SkillId SkillId;
    //public int SkillLvl; // needed?
    public SkillFailReason Reason;
}

public class GridEntityDataPacket : Packet
{
    public override int PacketType => 29;

    public int UnitId;
    public string UnitName;
    public string MapId;
    public GridData.Path Path;
    public int PathCellIndex;
    public float Movespeed;
    public float MovementCooldown;
    public GridData.Direction Orientation; // can mostly be inferred from movement, but units who haven't moved may need it
    public Vector2Int Coordinates; // for units that don't have a path right now
}

public class BattleEntityDataPacket : Packet
{
    public override int PacketType => 30;

    public int UnitId;
    public string UnitName;
    public string MapId;
    public GridData.Path Path;
    public int PathCellIndex;
    public float Movespeed;
    public float MovementCooldown;
    public GridData.Direction Orientation; // can mostly be inferred from movement, but units who haven't moved may need it
    public Vector2Int Coordinates; // for units that don't have a path right now

    public int MaxHp;
    public int Hp;
    public int MaxSp;
    public int Sp;
}

public class RemoteCharacterDataPacket : Packet
{
    public override int PacketType => 31;

    public int UnitId;
    public string UnitName;
    public string MapId;
    public GridData.Path Path;
    public int PathCellIndex;
    public float Movespeed;
    public float MovementCooldown;
    public GridData.Direction Orientation; // can mostly be inferred from movement, but units who haven't moved may need it
    public Vector2Int Coordinates; // for units that don't have a path right now

    public int MaxHp;
    public int Hp;
    public int MaxSp;
    public int Sp;

    public int BaseLvl;
    public JobId JobId;
    public int Gender;
    // TODO: Cosmetic info

}

public class LocalCharacterDataPacket : Packet
{
    public override int PacketType => 32;

    public int UnitId;
    public string UnitName;
    public string MapId;
    public GridData.Path Path;
    public int PathCellIndex;
    public float Movespeed;
    public float MovementCooldown;
    public GridData.Direction Orientation; // can mostly be inferred from movement, but units who haven't moved may need it
    public Vector2Int Coordinates; // for units that don't have a path right now

    public int MaxHp;
    public int Hp;
    public int MaxSp;
    public int Sp;

    public int BaseLvl;
    public JobId JobId;
    public int JobLvl;
    public int Gender;

    // Stats the player sees
    // Send them as stats so the UI can split modifiers from base if wanted
    public Stat Str;
    public Stat Agi;
    public Stat Vit;
    public Stat Int;
    public Stat Dex;
    public Stat Luk;

    public Stat AtkMin;
    public Stat AtkMax;
    public Stat MatkMin;
    public Stat MatkMax;
    public StatFloat HardDef;
    public Stat SoftDef;
    public StatFloat HardMdef;
    public Stat SoftMdef;
    public Stat Hit;
    public StatFloat PerfectHit;
    public Stat Flee;
    public StatFloat PerfectFlee;
    public StatFloat Crit;
    public float AttackSpeed; // What's the format for this gonna be? Attacks/Second? Should I use a Stat for this?
    public Stat Weightlimit;

    public int RemainingSkillPoints;
    public int RemainingStatPoints;

    public int StrIncreaseCost;
    public int AgiIncreaseCost;
    public int VitIncreaseCost;
    public int IntIncreaseCost;
    public int DexIncreaseCost;
    public int LukIncreaseCost;

    public int CurrentBaseExp;
    public int RequiredBaseExp;
    public int CurrentJobExp;
    public int RequiredJobExp;

    // TODO: cosmetic, equipment?
}

public class StatUpdatePacket : Packet
{
    public override int PacketType => 37;

    public EntityPropertyType Type;
    public Stat NewValue;
}

public class StatFloatUpdatePacket : Packet
{
    public override int PacketType => 40;

    public EntityPropertyType Type;
    public StatFloat NewValue;
}

public class StatCostUpdatePacket : Packet
{
    public override int PacketType => 41;

    public EntityPropertyType Type;
    public int NewCost;
}

public class StatPointUpdatePacket : Packet
{
    public override int PacketType => 42;

    public int NewRemaining;
}

public class ExpUpdatePacket : Packet
{
    public override int PacketType => 33;

    public int CurrentBaseExp;
    public int CurrentJobExp;
}

public class InventoryStatePacket : Packet
{
    public override int PacketType => 38;
}

public class ItemReceivedPacket : Packet
{
    public override int PacketType => 34;

    public bool IsPickup; // for playing pickup-animation
}

public class EquipChangedPacket : Packet
{
    public override int PacketType => 35;
}

// Do we use this for the local player as well, or only for remote ones?
public class PlayerEquipmentInfoPacket : Packet
{
    public override int PacketType => 36;
}

public class StatIncreaseRequestPacket : Packet
{
    public override int PacketType => 39;

    public EntityPropertyType StatType;
    // public int IncreaseAmount // probably only necessary if UI is changed to allow multi-increase
}

public class LevelUpPacket : Packet
{
    public override int PacketType => 43;

    public int EntityId;
    public int Level;
    public bool IsJob;
    public int RequiredExp;
}

public class AccountCreationRequestPacket : Packet
{
    public override int PacketType => 44;

    public string Username;
    public string Password;
}

public class AccountCreationResponsePacket : Packet
{
    public override int PacketType => 45;

    public int Result;
}

public class AccountDeletionRequestPacket : Packet
{
    public override int PacketType => 48;

    public string AccountId;
}

public class AccountDeletionResponsePacket : Packet
{
    public override int PacketType => 49;

    public int Result;
}

public class CharacterCreationRequestPacket : Packet
{
    public override int PacketType => 46;

    public string Name;
    public int Gender;
    // TODO: Other configuration at Character creation
}

public class CharacterCreationResponsePacket : Packet
{
    public override int PacketType => 47;

    public int Result;
}

public class CharacterDeletionRequestPacket : Packet
{
    public override int PacketType => 50;

    public int CharacterId;
}

public class CharacterDeletionResponsePacket : Packet
{
    public override int PacketType => 51;

    public int Result;
}

public class LocalPlayerEntitySkillQueuedPacket : Packet
{
    public override int PacketType => 52;

    public SkillId SkillId;
    public int TargetId;
}

public class LocalPlayerGroundSkillQueuedPacket : Packet
{
    public override int PacketType => 53;

    public SkillId SkillId;
    public Vector2Int Target;
}

public enum SkillCategory
{
    FirstClass,
    SecondClass,
    Temporary
}

public class SkillTreeEntryPacket : Packet
{
    public override int PacketType => 54;

    public SkillId SkillId;
    public int Tier;
    public int Position;
    public int MaxSkillLvl;
    public int LearnedSkillLvl;
    public bool CanPointLearn;
    public SkillCategory Category = SkillCategory.FirstClass;
    public SkillId Requirement1Id = SkillId.Unknown;
    public int Requirement1Level = -1;
    public SkillId Requirement2Id = SkillId.Unknown;
    public int Requirement2Level = -1;
    public SkillId Requirement3Id = SkillId.Unknown;
    public int Requirement3Level = -1;
}

public class SkillTreeRemovePacket : Packet
{
    public override int PacketType => 55;

    public SkillId SkillId;
}

public class SkillPointAllocateRequestPacket : Packet
{
    public override int PacketType => 56;

    public SkillId SkillId;
    public int Amount;

    // Some form of batching-support here, to send fewer SkillPointAllocateResponses?
}

public class SkillPointUpdatePacket : Packet
{
    public override int PacketType => 57;

    public int RemainingSkillPoints;
}

public class ReturnAfterDeathRequestPacket : Packet
{
    public override int PacketType => 58;

    public int CharacterId;
}

// Move & adjust this comment when adding new packets, to make dev easier
// Next PacketType = 59
// Unused Ids: 8, 9, 12, 15