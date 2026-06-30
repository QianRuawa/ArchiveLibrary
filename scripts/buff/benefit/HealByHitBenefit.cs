using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 受击回复：持有者每次受到伤害时获得等量层数治疗，
/// 然后消耗 1 层可消耗。初始获取时同时获得 1 层治疗和 1 层可消耗，
/// 后续叠加只增加治疗层，可通过 <see cref="AddCharges"/> 单独增加可消耗。
/// 左上角额外角标显示剩余可消耗次数。
/// </summary>
[RegisterPower]
public class HealByHitBenefit : ModBadgePower
{
    public override string IconId => "HealByHit_Damaged";
    public override PowerCategory Category => PowerCategory.Benefit;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _charges;

    public int Charges => _charges;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Charges", 0m)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _charges = (int)Amount;
        SyncChargesVar();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            InvokeDisplayAmountChanged();
            Flash();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Owner.IsDead) return;
        if (Amount <= 0 || _charges <= 0) return;
        if (result.UnblockedDamage <= 0) return;

        Flash();
        await CreatureCmd.Heal(Owner, Amount);

        _charges--;
        if (_charges <= 0)
            await PowerCmd.Remove(this);
        else
        {
            SyncChargesVar();
            InvokeDisplayAmountChanged();
        }
    }

    public void AddCharges(int count)
    {
        _charges += count;
        SyncChargesVar();
        InvokeDisplayAmountChanged();
    }

    private void SyncChargesVar()
    {
        DynamicVars["Charges"].BaseValue = _charges;
    }

    // ===== 角标：左下显示剩余次数 =====
    protected override IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerBadgeSpecs()
    {
        return
        [
            MakeBadge(ExtraIconAmountLabelCorner.BottomLeft, _charges.ToString()),
        ];
    }

    // ===== 静态 API =====

    /// <summary>给予受击回复能力。</summary>
    /// <param name="charges">可消耗次数。</param>
    /// <param name="healAmount">每次触发回复量。</param>
    public static async Task ApplyHealByHit(PlayerChoiceContext ctx, Creature target, int charges, int healAmount)
    {
        // 先以 charges 设定 _charges（AfterApplied 会设 _charges = Amount）
        await PowerCmd.Apply(ctx, ModelDb.DebugPower(typeof(HealByHitBenefit)).ToMutable(), target, charges, null, null);
        // 再调整 Amount 到实际治疗量
        if (healAmount != charges)
        {
            var p = target.GetPower<HealByHitBenefit>();
            if (p != null)
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), p, healAmount - charges, null, null);
        }
    }
}
