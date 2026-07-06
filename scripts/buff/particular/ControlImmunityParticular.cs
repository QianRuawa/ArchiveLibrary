using ArchiveLibrary.Scripts.Patches;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 控制免疫：持有者免疫所有控制状态（击晕、强制结束回合、恐惧等 CC 效果）。
/// 同时自动免疫击晕（通过 <see cref="IStunImmunePower"/> 接口）。
/// </summary>
[RegisterPower]
public class ControlImmunityParticular : ModPowerTemplate, IPowerCategorizable, IStunImmunePower
{
    public string IconId => "ImmuneCC";
    public PowerCategory Category => PowerCategory.Particular;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
