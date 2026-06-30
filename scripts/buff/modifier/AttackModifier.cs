using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 攻击修正：正数增伤，负数减伤。与 Slay the Spire 2 原版力量（StrengthPower）机制一致。
/// 标题/描述/图标/标签颜色均由 <see cref="ModDynamicIconPower"/> 基类根据 Amount 符号自动处理。
/// </summary>
[RegisterPower]
public class AttackModifier : ModDynamicIconPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string IconId => "ATK";
    public override PowerCategory Category => PowerCategory.Benefit;

    /// <summary>对持有者自身的攻击伤害修正 Amount 值（可为负，表示减伤）。</summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != dealer) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        return Amount;
    }
}
