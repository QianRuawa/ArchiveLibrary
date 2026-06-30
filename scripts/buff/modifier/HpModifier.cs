using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 生命值修正：正数获取额外血量（吸收盾，优先于实际 HP 承受伤害），
/// 负数回合结束扣除血量并清除。
/// 标题/描述/图标/标签颜色均由 <see cref="ModDynamicIconPower"/> 基类根据 Amount 符号自动处理。
/// </summary>
[RegisterPower]
public class HpModifier : ModDynamicIconPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string IconId => "MAXHP";
    public override PowerCategory Category => PowerCategory.Benefit;

    // HpModifier 正负描述完全不同，需要分开
    protected override string SmartDescriptionLocKey => Amount >= 0
        ? Id.Entry + ".smartDescription"
        : Id.Entry + ".smartDescription.negative";

    private int _pendingDecrement;

    /// <summary>获取正面额外血量时播放回血特效（绿色十字光效）。</summary>
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount > 0 && Owner != null)
            VfxCmd.PlayOnCreatureCenter(Owner, "vfx/vfx_cross_heal");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 正面效果：在格挡之后、实际 HP 之前吸收非固定伤害。
    /// 层数即剩余额外血量，被攻击时层数减少，扣血优先消耗此层数而非实际 HP。
    /// </summary>
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0)
            return amount;

        if (props.HasFlag(ValueProp.Unblockable))
            return amount;

        int absorb = (int)Math.Min(Amount, amount);
        if (absorb > 0)
        {
            _pendingDecrement = absorb;
            return amount - absorb;
        }
        return amount;
    }

    /// <summary>将暂存的吸收量从层数中扣除（静默，不触发飘字特效）。</summary>
    public override Task AfterModifyingHpLostAfterOsty()
    {
        if (_pendingDecrement > 0)
        {
            SetAmount(Amount - _pendingDecrement, silent: true);
            _pendingDecrement = 0;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 负面效果：回合结束扣除血量，清除。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.IsDead)
            return;
        if (Amount >= 0)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, -Amount, ValueProp.Unpowered, null, null);
        await PowerCmd.Remove(this);
    }
}
