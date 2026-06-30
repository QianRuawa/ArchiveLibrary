using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>中毒强化：本回合内中毒伤害 +50% 且不取半。</summary>
[RegisterPower]
public class PoisonBoostAdverse : DotBoostBase<PoisonAdverse>
{
    public override string IconId => "PoisonDamagedIncrease";
}
