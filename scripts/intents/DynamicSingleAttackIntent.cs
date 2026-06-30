using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ArchiveLibrary.Scripts.Intents;

/// <summary>动态伤害单段攻击，受力量等加成。</summary>
public class DynamicSingleAttackIntent : AttackIntent, ICustomIntentDescription
{
    public override int Repeats => 1;
    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_SINGLE");
    public string? CustomLocPrefix { get; set; }
    protected override string IntentPrefix => CustomLocPrefix ?? "ATTACK";

    public DynamicSingleAttackIntent(int damage)
    {
        DamageCalc = () => damage;
    }

    public DynamicSingleAttackIntent(Func<decimal> damageCalc)
    {
        DamageCalc = damageCalc;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
        => GetSingleDamage(targets, owner);

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var label = IntentLabelFormat;
        label.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        label.Add("Damage", GetTotalDamage(targets, owner));
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var desc = new LocString("intents", IntentPrefix + ".description");
        desc.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        desc.Add("Damage", GetSingleDamage(targets, owner));
        return desc;
    }

    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner)
    {
        int total = GetTotalDamage(targets, owner);
        string a = "attack";
        if (total < 5) return a + "_1";
        if (total < 10) return a + "_2";
        if (total < 20) return a + "_3";
        if (total < 40) return a + "_4";
        return a + "_5";
    }
}
