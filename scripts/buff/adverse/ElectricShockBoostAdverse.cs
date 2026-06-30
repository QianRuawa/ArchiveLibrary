using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>感电强化：本回合内感电伤害 +50% 且不取半。</summary>
[RegisterPower]
public class ElectricShockBoostAdverse : DotBoostBase<ElectricShockAdverse>
{
    public override string IconId => "ElectricShockDamagedIncrease";
}
