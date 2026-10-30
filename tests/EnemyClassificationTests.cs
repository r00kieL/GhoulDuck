using System;
using GhoulDuck;

internal static class EnemyClassificationTests
{
    private static int Main()
    {
        Check("Cname_Scav", false, EnemyType.Scavenger);
        Check("Cname_ScavRage", false, EnemyType.Scavenger);
        Check("Cname_Scav_Ice", false, EnemyType.Scavenger);
        Check("Cname_Usec", false, EnemyType.Mercenary);
        Check("Cname_Usec_Ice", false, EnemyType.Mercenary);
        Check("Cname_Raider", false, EnemyType.Raider);
        Check("Cname_RobSpider", false, EnemyType.Spider);
        Check("Cname_DengWolf", false, EnemyType.WolfOrDog);
        Check("Cname_Wolf_Ice", false, EnemyType.WolfOrDog);
        Check("Cname_Ghost", false, EnemyType.Ghost);
        Check("Cname_StormCreature", false, EnemyType.StormCreature);
        Check("Cname_Mushroom", false, EnemyType.Mushroom);
        Check("Cname_Wolf", false, EnemyType.Boss);
        Check("Cname_StormBoss1", false, EnemyType.Boss);
        Check("Cname_RobSpider", true, EnemyType.Boss);
        Check("Cname_Boss_Fly_Child", false, EnemyType.Unknown);
        Check("Cname_StormBoss1_Child", false, EnemyType.Unknown);
        Check("Cname_Unknown", false, EnemyType.Unknown);
        Check(null, false, EnemyType.Unknown);
        Console.WriteLine("Enemy classification checks passed.");
        return 0;
    }

    private static void Check(string name, bool boss, EnemyType expected)
    {
        var actual = EnemyClassification.Classify(name, boss);
        if (actual != expected)
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual);
    }
}
