using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 防御力修正：正数提升获取格挡值，负数降低获取格挡值。
/// 与 Slay the Spire 2 原版灵巧（DexterityPower）机制一致。
/// 标题/描述/图标/标签颜色均由 <see cref="ModDynamicIconPower"/> 基类根据 Amount 符号自动处理。
/// </summary>
[RegisterPower]
public class DefenseModifier : ModDynamicIconPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string IconId => "DEF";
    public override PowerCategory Category => PowerCategory.Benefit;

    /// <summary>对持有者自身的格挡值修正 Amount 值（可为负，表示减格挡）。</summary>
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner) return 0m;
        if (!props.IsPoweredCardOrMonsterMoveBlock()) return 0m;
        return Amount;
    }
}
