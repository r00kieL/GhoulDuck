using System;

namespace GhoulDuck
{
    internal enum EnemyType
    {
        Unknown, Scavenger, Mercenary, Raider, Boss, Spider, WolfOrDog, Ghost, StormCreature, Mushroom
    }

    internal static class EnemyClassification
    {
        // Names are the game's stable localization keys, not translated display names.
        internal static EnemyType Classify(string nameKey, bool isBoss)
        {
            if (isBoss) return EnemyType.Boss;
            switch (nameKey ?? string.Empty)
            {
                case "Cname_Wolf": case "Cname_ShortEagle": case "Cname_UltraMan":
                case "Cname_CrazyRob": case "Cname_Boss_Sniper": case "Cname_Boss_Shot":
                case "Cname_ServerGuardian": case "Cname_Speedy": case "Cname_Prison_Boss":
                case "Cname_BALeader": case "Cname_Boss_Fly": case "Cname_Boss_Arcade":
                case "Cname_Boss_3Shot": case "Cname_StormBoss1": case "Cname_StormBoss2":
                case "Cname_StormBoss3": case "Cname_StormBoss4": case "Cname_StormBoss5":
                case "Cname_Vida": case "Cname_WolfKing_Ice": case "Cname_Speedy_Ice":
                case "Cname_Boss_Red": case "Cname_Boss_Blue": case "Cname_Boss_LionHead":
                case "Cname_IslandBoss": case "Cname_Tagilla": case "Cname_Killa":
                    return EnemyType.Boss;
                case "Cname_Scav": case "Cname_ScavRage": case "Cname_Scav_Ice":
                    return EnemyType.Scavenger;
                case "Cname_Usec": case "Cname_Usec_Ice":
                    return EnemyType.Mercenary;
                case "Cname_Raider": return EnemyType.Raider;
                case "Cname_RobSpider": return EnemyType.Spider;
                case "Cname_DengWolf": case "Cname_Wolf_Ice": return EnemyType.WolfOrDog;
                case "Cname_Ghost": return EnemyType.Ghost;
                case "Cname_StormCreature": return EnemyType.StormCreature;
                case "Cname_Mushroom": return EnemyType.Mushroom;
                default: return EnemyType.Unknown;
            }
        }
    }
}
