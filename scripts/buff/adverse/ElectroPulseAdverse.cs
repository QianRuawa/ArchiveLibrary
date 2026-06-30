using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>电磁脉冲：回合结束时造成等量层数伤害，然后移除。</summary>
[RegisterPower]
public class ElectroPulseAdverse : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "ElectroPulse";
    public PowerCategory Category => PowerCategory.Adverse;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.IsDead || Amount <= 0)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, null, null);
        await PowerCmd.Remove(this);
    }
}
