using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 持续伤害（DoT）基类：回合结束时造成等量层数伤害，后向下取半。
/// 对应强化存在时伤害 +50% 且不取半，强化由子类指定。
/// </summary>
public abstract class DotDebuffBase<TBoost> : ModPowerTemplate, IPowerCategorizable
    where TBoost : PowerModel
{
    public abstract string IconId { get; }
    public PowerCategory Category => PowerCategory.Adverse;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override string SmartDescriptionLocKey =>
        Owner?.HasPower<TBoost>() == true
            ? Id.Entry + ".smartDescription.boosted"
            : Id.Entry + ".smartDescription";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BoostedDamage", 0m)
    ];

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars["BoostedDamage"].BaseValue = Amount * 1.5m;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.IsDead || Amount <= 0)
            return;

        bool boosted = Owner.HasPower<TBoost>();
        decimal damage = boosted ? Amount * 1.5m : Amount;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, damage, ValueProp.Unpowered, null, null);

        if (boosted)
        {
            var boost = Owner.GetPower<TBoost>();
            if (boost != null)
                await PowerCmd.Remove(boost);
        }
        else if (Owner.IsAlive)
        {
            decimal newAmount = Math.Floor(Amount / 2m);
            await PowerCmd.ModifyAmount(choiceContext, this, newAmount - Amount, null, null);
        }
    }
}
