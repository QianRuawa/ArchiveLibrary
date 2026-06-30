using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 回复限制：持有者单次得到的治疗量最多为 Amount 点（每层+1）。
/// </summary>
[RegisterPower]
public class HealLimitAdverse : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "HealedLimit";
    public PowerCategory Category => PowerCategory.Adverse;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
