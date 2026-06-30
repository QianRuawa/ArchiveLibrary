using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 沉默：持有者无法使用技能牌（Skill），回合结束时移除。
/// </summary>
[RegisterPower]
public class SilenceCrowdControl : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "Silence";
    public override PowerType Type => PowerType.Debuff;
    public PowerCategory Category => PowerCategory.CrowdControl;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    /// <summary>阻止技能牌使用。</summary>
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card?.Owner?.Creature != Owner) return true;
        if (card.Type == CardType.Skill) return false;
        return true;
    }

    /// <summary>回合结束时移除沉默。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
