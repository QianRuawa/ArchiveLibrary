using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 不死：生命值不会低于 2 点，回合开始时自动移除。
/// 若需要跨回合持续，设置 <see cref="SpecialActivation"/> = true。
/// </summary>
[RegisterPower]
public class ImmortalParticular : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "Immortal";
    public PowerCategory Category => PowerCategory.Particular;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>设置为 true 时不会在回合开始时自动移除。</summary>
    public virtual bool SpecialActivation { get; set; } = false;

    /// <summary>生命值不会低于 2 点。</summary>
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        if (Owner.CurrentHp - amount <= 2m) return Owner.CurrentHp - 2m;
        return amount;
    }

    /// <summary>非特殊激活时，回合开始自动移除。</summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (!SpecialActivation)
            await PowerCmd.Remove(this);
    }
}
