using STS2RitsuLib.Interop.AutoRegistration;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>恶寒：回合结束时造成等量层数伤害，后向下取半。</summary>
[RegisterPower]
public class ChillAdverse : DotDebuffBase<ChillBoostAdverse>
{
    public override string IconId => "Chill";
}
