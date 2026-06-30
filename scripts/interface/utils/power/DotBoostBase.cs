using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 持续伤害强化基类：本回合内对应的 DoT 伤害 +50% 且不取半。
/// 对应 DoT 触发后会自动移除。若没有对应 DoT 则回合开始清理。
/// </summary>
public abstract class DotBoostBase<TDot> : ModPowerTemplate, IPowerCategorizable
    where TDot : PowerModel
{
    public abstract string IconId { get; }
    public PowerCategory Category => PowerCategory.Adverse;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (!Owner.HasPower<TDot>())
            await PowerCmd.Remove(this);
    }
}
