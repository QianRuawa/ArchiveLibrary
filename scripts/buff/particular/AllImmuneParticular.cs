using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ArchiveLibrary.Scripts.Visual;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 什亭之匣：持有者免疫所有攻击伤害和施加控制或正负面状态的效果，无法被常规手段移除。
/// 特定逻辑可通过 <see cref="AllowRemoveOnce"/> 允许一次移除。
/// </summary>
[RegisterPower]
public class AllImmuneParticular : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "AllMiss";
    public PowerCategory Category => PowerCategory.Particular;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>是否允许下一次移除操作（特定逻辑使用）。</summary>
    internal static bool AllowRemoveOnce { get; set; }

    /// <summary>免疫所有伤害。</summary>
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m) return amount;
        Flash();
        NImmuneVfx.Display(Owner, EntiyArchiveLibrary.UI.GetText("IMMUNE_ALL"));
        return 0m;
    }
}
