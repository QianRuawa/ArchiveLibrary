using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 护盾：持续期间为持有者抵挡所有伤害，层数随吸收伤害减少，归零时破碎。
/// 若攻击者贯穿层数 > 护盾层数，则护盾被无视，伤害直接穿透。
/// Unblockable 伤害可穿透护盾。
/// </summary>
[RegisterPower]
public class ShieldBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "Shield";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _pendingDecrement;

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0)
            return amount;

        if (props.HasFlag(ValueProp.Unblockable))
            return amount;

        // 贯穿 > 护盾层数 → 护盾被无视
        if (PenetrationBenefit.PendingTarget == target && PenetrationBenefit.PendingAmount > Amount)
            return amount;

        int absorbed = (int)Math.Min(Amount, amount);
        if (absorbed > 0)
        {
            _pendingDecrement = absorbed;
            return amount - absorbed;
        }
        return amount;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (_pendingDecrement > 0)
        {
            await PowerCmd.ModifyAmount(null, this, -_pendingDecrement, null, null);
            _pendingDecrement = 0;
        }
    }
}
