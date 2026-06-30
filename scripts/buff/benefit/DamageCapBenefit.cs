using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 承伤限制：持有者单次受到的伤害最多为当前层数。
/// 与杀戮尖塔2原版"难以杀灭"(HardToKill)机制一致。
/// </summary>
[RegisterPower]
public class DamageCapBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "DamagedLimit";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>单次受到的伤害不会超过 Amount 点。</summary>
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return decimal.MaxValue;
        return Amount;
    }
}
