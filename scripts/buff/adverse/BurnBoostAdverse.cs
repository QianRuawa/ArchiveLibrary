using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>燃烧强化：本回合内燃烧伤害 +50% 且不取半。</summary>
[RegisterPower]
public class BurnBoostAdverse : DotBoostBase<BurnAdverse>
{
    public override string IconId => "BurnDamagedIncrease";
}
