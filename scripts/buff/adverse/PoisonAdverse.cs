using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>中毒：回合结束时造成等量层数伤害，后向下取半。</summary>
[RegisterPower]
public class PoisonAdverse : DotDebuffBase<PoisonBoostAdverse>
{
    public override string IconId => "Poison";
}
