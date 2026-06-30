using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 全局伤害率修正：正数提升造成伤害（Buff），负数降低造成伤害（Debuff）。
/// 注意："降低"才是减益（Amount 为负时）。
/// 每层修正 <see cref="MultiplierPerStack"/>%（默认 5%），与力量等增伤独立乘算。
/// 标题/描述/图标/标签颜色均由 <see cref="ModDynamicIconPower"/> 基类根据 Amount 符号自动处理。
/// </summary>
[RegisterPower]
public class DamageOutputModifier : ModDynamicIconPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string IconId => "DamageRatio";
    public override PowerCategory Category => PowerCategory.Benefit;

    private decimal _multiplierPerStack = 0.05m;

    /// <summary>每层修正倍率（默认 0.05 = 5%）。</summary>
    public decimal MultiplierPerStack => _multiplierPerStack;

    /// <summary>运行时修改每层倍率。</summary>
    public void SetMultiplierPerStack(decimal multiplier)
    {
        _multiplierPerStack = multiplier;
        UpdateTotalPercent();
        InvokeDisplayAmountChanged();
    }

    /// <summary>图标上显示百分比而非原始层数。</summary>
    public override int DisplayAmount => (int)(Math.Abs(Amount) * _multiplierPerStack * 100);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TotalPercent", 0m)
    ];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            UpdateTotalPercent();
            InvokeDisplayAmountChanged();
            Flash();
        }
    }

    private void UpdateTotalPercent()
    {
        DynamicVars["TotalPercent"].BaseValue = DisplayAmount;
    }

    /// <summary>
    /// Amount > 0（Buff）→ 提升造成伤害，multiplier &gt; 1.0<br/>
    /// Amount &lt; 0（Debuff）→ 降低造成伤害，multiplier &lt; 1.0
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        decimal mult = 1m + Amount * _multiplierPerStack;
        return Math.Max(mult, 0.05m);
    }

    /// <summary>触发伤害修正后闪烁图标。</summary>
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        return Task.CompletedTask;
    }
}
