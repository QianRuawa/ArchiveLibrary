using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ArchiveLibrary.Scripts.Intents;

/// <summary>固定伤害单段攻击，不受力量加成。</summary>
public class FixedSingleAttackIntent : AttackIntent, ICustomIntentDescription
{
    public override int Repeats => 1;
    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_SINGLE");

    private readonly int _damage;
    public string? CustomLocPrefix { get; set; }
    protected override string IntentPrefix => CustomLocPrefix ?? "ATTACK";

    public FixedSingleAttackIntent(int damage)
    {
        _damage = damage;
        DamageCalc = () => damage;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
        => _damage;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var label = IntentLabelFormat;
        label.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        label.Add("Damage", _damage);
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var desc = IntentDescriptionHelper.GetDescription(CustomLocPrefix, "ATTACK.description");
        desc.Add("IsMultiplayer", owner?.CombatState?.RunState.Players.Count > 1);
        desc.Add("Damage", _damage);
        desc.Add("Repeat", 1);
        return desc;
    }

    // 强制使用 "attack" 前缀加载图标帧，避免 CustomLocPrefix 影响图标
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
