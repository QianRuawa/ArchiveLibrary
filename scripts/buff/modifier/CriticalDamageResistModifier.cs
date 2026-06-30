using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 暴击伤害抵抗修正：正数增加目标受到的暴击伤害（易伤），负数减少目标受到的暴击伤害（抵抗）。
/// 被 <see cref="CriticalChanceBenefit"/> 读取以调整暴击倍率。
/// </summary>
[RegisterPower]
public class CriticalDamageResistModifier : ModDynamicIconPower
{
    public override string IconId => "CriticalDamageRateResist";
    public override PowerCategory Category => PowerCategory.Benefit;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private decimal _multiplierPerStack = 0.05m;

    public decimal MultiplierPerStack => _multiplierPerStack;

    public void SetMultiplierPerStack(decimal multiplier)
    {
        _multiplierPerStack = multiplier;
        UpdateTotalPercent();
        InvokeDisplayAmountChanged();
    }

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
            if (Owner != null)
                Owner.GetPower<CriticalChanceBenefit>()?.RefreshCritDisplay();
        }
    }

    private void UpdateTotalPercent()
    {
        DynamicVars["TotalPercent"].BaseValue = DisplayAmount;
    }
}
