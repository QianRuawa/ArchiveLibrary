using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ArchiveLibrary.Scripts.Intents;

/// <summary>动态伤害多段攻击，受力量等加成。</summary>
public class DynamicMultiAttackIntent : AttackIntent, ICustomIntentDescription
{
    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_MULTI");

    private readonly int _repeat;
    private readonly Func<int>? _repeatCalc;

    public override int Repeats => _repeatCalc?.Invoke() ?? _repeat;
    public string? CustomLocPrefix { get; set; }
    protected override string IntentPrefix => CustomLocPrefix ?? "ATTACK";

    public DynamicMultiAttackIntent(int damage, int repeat)
    {
        _repeat = repeat;
        _repeatCalc = null;
        DamageCalc = () => damage;
    }

    public DynamicMultiAttackIntent(int damage, Func<int> repeatCalc)
    {
        _repeat = 0;
        _repeatCalc = repeatCalc;
        DamageCalc = () => damage;
    }

    public DynamicMultiAttackIntent(Func<decimal> damageCalc, int repeat)
    {
        _repeat = repeat;
        _repeatCalc = null;
        DamageCalc = damageCalc;
    }

    public DynamicMultiAttackIntent(Func<decimal> damageCalc, Func<int> repeatCalc)
    {
        _repeat = 0;
        _repeatCalc = repeatCalc;
        DamageCalc = damageCalc;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
        => GetSingleDamage(targets, owner) * Repeats;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var label = IntentLabelFormat;
        label.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        label.Add("Damage", (int)GetSingleDamage(targets, owner));
        label.Add("Repeat", Repeats);
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var desc = new LocString("intents", IntentPrefix + ".description");
        desc.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        desc.Add("Damage", (int)GetSingleDamage(targets, owner));
        desc.Add("Repeat", Repeats);
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
