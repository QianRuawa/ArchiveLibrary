using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>感电：回合结束时造成等量层数伤害，后向下取半。</summary>
[RegisterPower]
public class ElectricShockAdverse : DotDebuffBase<ElectricShockBoostAdverse>
{
    public override string IconId => "ElectricShock";
}
