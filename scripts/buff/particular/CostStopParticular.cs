using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// COST 回复停止：持有者无法回复能量，回合结束时移除。
/// </summary>
[RegisterPower]
public class CostStopParticular : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "CostStop";
    public PowerCategory Category => PowerCategory.Particular;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>阻止能量回复。</summary>
    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player.Creature == Owner) return 0m;
        return amount;
    }

    /// <summary>回合结束时移除。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
