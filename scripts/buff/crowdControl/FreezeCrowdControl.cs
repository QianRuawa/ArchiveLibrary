using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 冻结：持有者累积层数达到阈值时强制结束本回合（默认 3 层触发）。
/// 可在 <see cref="AssetProfile"/> 中自定义 <c>FreezeThreshold</c> 参数修改阈值。
/// </summary>
[RegisterPower]
public class FreezeCrowdControl : ModPowerTemplate, IPowerCategorizable
{
    /// <summary>触发冻结的层数阈值（默认 3）。</summary>
    public int FreezeThreshold { get; set; } = 3;

    public string IconId => "Frozen";
    public override PowerType Type => PowerType.Debuff;
    public PowerCategory Category => PowerCategory.CrowdControl;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    /// <summary>累积层数 ≥ 阈值时结束本回合。触发后移除。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        if (Amount < FreezeThreshold) return;
        Flash();
        await PowerCmd.Remove(this);
        var player = Owner.Player;
        if (player != null)
            PlayerCmd.EndTurn(player, canBackOut: false);
    }
}
