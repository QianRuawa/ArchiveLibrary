using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// COST超载：持有者在回复能量时被扣除透支额，直到归零。
/// Amount = 剩余待还透支额，归零时自动移除。
/// </summary>
[RegisterPower]
public class CostOverloadBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "CostOverload";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>回合开始时从回复能量中扣除透支额</summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !Owner.IsPlayer || Amount <= 0)
            return;

        int energy = Owner.Player?.PlayerCombatState?.Energy ?? 0;
        int deduct = Math.Min(Amount, energy);
        if (deduct <= 0) return;

        Owner.Player.PlayerCombatState.LoseEnergy(deduct);
        if (Amount - deduct <= 0)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -deduct, null, null);
    }
}
