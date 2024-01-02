
using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace Client
{
    public class GridEntityData
    {
        public int UnitId;
        public string UnitName;
        public string MapId;
        public GridData.Path Path;
        public int PathCellIndex;
        public float Movespeed;
        public float MovementCooldown;
        public GridData.Direction Orientation; // can mostly be inferred from movement, but units who haven't moved may need it
        public Vector2Int Coordinates; // for units that don't have a path right now

        public static GridEntityData FromPacket(GridEntityDataPacket packet)
        {
            GridEntityData result = new();
            result.UpdateFromPacket(packet);
            return result;
        }

        public void UpdateFromPacket(GridEntityDataPacket packet)
        {
            UnitId = packet.UnitId;
            UnitName = packet.UnitName;
            MapId = packet.MapId;
            Path = packet.Path;
            PathCellIndex = packet.PathCellIndex;
            Movespeed = packet.Movespeed;
            MovementCooldown = packet.MovementCooldown;
            Orientation = packet.Orientation;
            Coordinates = packet.Coordinates;
        }
    }

    public class BattleEntityData : GridEntityData
    {
        public int MaxHp;
        public int Hp;
        public int MaxSp;
        public int Sp;

        public static BattleEntityData FromPacket(BattleEntityDataPacket packet)
        {
            BattleEntityData result = new();
            result.UpdateFromPacket(packet);
            return result;
        }

        public void UpdateFromPacket(BattleEntityDataPacket packet)
        {
            UnitId = packet.UnitId;
            UnitName = packet.UnitName;
            MapId = packet.MapId;
            Path = packet.Path;
            PathCellIndex = packet.PathCellIndex;
            Movespeed = packet.Movespeed;
            MovementCooldown = packet.MovementCooldown;
            Orientation = packet.Orientation;
            Coordinates = packet.Coordinates;

            MaxHp = packet.MaxHp;
            Hp = packet.Hp;
            MaxSp = packet.MaxSp;
            Sp = packet.Sp;
        }
    }

    public class RemoteCharacterData : BattleEntityData
    {
        public int BaseLvl;
        public JobId JobId;
        public int Gender;
        
        public static RemoteCharacterData FromPacket(RemoteCharacterDataPacket packet)
        {
            RemoteCharacterData result = new();
            result.UpdateFromPacket(packet);
            return result;
        }

        public void UpdateFromPacket(RemoteCharacterDataPacket packet)
        {
            UnitId = packet.UnitId;
            UnitName = packet.UnitName;
            MapId = packet.MapId;
            Path = packet.Path;
            PathCellIndex = packet.PathCellIndex;
            Movespeed = packet.Movespeed;
            MovementCooldown = packet.MovementCooldown;
            Orientation = packet.Orientation;
            Coordinates = packet.Coordinates;

            MaxHp = packet.MaxHp;
            Hp = packet.Hp;
            MaxSp = packet.MaxSp;
            Sp = packet.Sp;

            BaseLvl = packet.BaseLvl;
            JobId = packet.JobId;
            Gender = packet.Gender;
        }
    }

    public class LocalCharacterData : RemoteCharacterData // If necessary, inherit from BattleEntityData instead
    {
        public int JobLvl;

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
        public int RemainingStatPoints;
        public int RemainingSkillPoints;
        public Stat Weightlimit;

        public int CurrentBaseExp;
        public int RequiredBaseExp;
        public int CurrentJobExp;
        public int RequiredJobExp;

        public int StrIncreaseCost;
        public int AgiIncreaseCost;
        public int VitIncreaseCost;
        public int IntIncreaseCost;
        public int DexIncreaseCost;
        public int LukIncreaseCost;


        public static LocalCharacterData FromPacket(LocalCharacterDataPacket packet)
        {
            LocalCharacterData data = new();
            data.UpdateFromPacket(packet);
            return data;
        }

        public void UpdateFromPacket(LocalCharacterDataPacket packet)
        {
            if(packet == null)
            {
                OwlLogger.LogError($"Tried to update LocalCharacterData from null packet!", GameComponent.Other);
                return;
            }

            UnitId = packet.UnitId;
            UnitName = packet.UnitName;
            MapId = packet.MapId;
            Path = packet.Path;
            PathCellIndex = packet.PathCellIndex;
            Movespeed = packet.Movespeed;
            MovementCooldown = packet.MovementCooldown;
            Orientation = packet.Orientation;
            Coordinates = packet.Coordinates;

            MaxHp = packet.MaxHp;
            Hp = packet.Hp;
            MaxSp = packet.MaxSp;
            Sp = packet.Sp;

            BaseLvl = packet.BaseLvl;
            JobId = packet.JobId;
            JobLvl = packet.JobLvl;
            Gender = packet.Gender;

            Str = packet.Str;
            Agi = packet.Agi;
            Vit = packet.Vit;
            Int = packet.Int;
            Dex = packet.Dex;
            Luk = packet.Luk;

            MatkMin = packet.MatkMin;
            MatkMax = packet.MatkMax;
            HardDef = packet.HardDef;
            SoftDef = packet.SoftDef;
            HardMdef = packet.HardMdef;
            SoftMdef = packet.SoftMdef;
            Hit = packet.Hit;
            PerfectHit = packet.PerfectHit;
            Flee = packet.Flee;
            PerfectFlee = packet.PerfectFlee;
            Crit = packet.Crit;
            //AttackSpeed = GetDefaultAnimationCooldown(); // TODO: format for this value, overriding of AnimationSpeed, etc.
            RemainingStatPoints = packet.RemainingStatPoints;
            RemainingSkillPoints = packet.RemainingSkillPoints;
            Weightlimit = packet.Weightlimit;

            AtkMin = packet.AtkMin;
            AtkMax = packet.AtkMax;

            CurrentBaseExp = packet.CurrentBaseExp;
            RequiredBaseExp = packet.RequiredBaseExp;
            CurrentJobExp = packet.CurrentJobExp;
            RequiredJobExp = packet.RequiredJobExp;

            StrIncreaseCost = packet.StrIncreaseCost;
            AgiIncreaseCost = packet.AgiIncreaseCost;
            VitIncreaseCost = packet.VitIncreaseCost;
            IntIncreaseCost = packet.IntIncreaseCost;
            DexIncreaseCost = packet.DexIncreaseCost;
            LukIncreaseCost = packet.LukIncreaseCost;
        }
    }
}

