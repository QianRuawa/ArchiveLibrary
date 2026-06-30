using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>恶寒强化：本回合内恶寒伤害 +50% 且不取半。</summary>
[RegisterPower]
public class ChillBoostAdverse : DotBoostBase<ChillAdverse>
{
    public override string IconId => "ChillDamagedIncrease";
}
