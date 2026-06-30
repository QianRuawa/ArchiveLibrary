using ArchiveLibrary.Scripts.Visual;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 恐惧：持有者会被击退且远离施加者，获取格挡减少20%。
/// 施加时按本次获得的层数击退（每层20像素，上限200），累计击退距离（上限200）。
/// 回合结束时移回原位并清除全部恐惧层数。
/// </summary>
[RegisterPower]
public class FearCrowdControl : ModPowerTemplate, IPowerCategorizable
{
    private const float KnockbackPerStack = 20f;
    private const float MaxTotalKnockback = 200f;
    private const decimal BlockMultiplier = 0.8m;

    private float _totalKnockback;
    private float _originalX;
    private bool _positionSaved;

    public string IconId => "Fear";
    public override PowerType Type => PowerType.Debuff;
    public PowerCategory Category => PowerCategory.CrowdControl;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    /// <summary>当施加者为 null（如控制台命令）时，以玩家生物作为击退方向来源。</summary>
    private Creature? ResolveApplier(Creature? applier)
    {
        if (applier != null) return applier;
        if (Owner == null) return null;
        if (Owner.Side == CombatSide.Enemy)
            return CombatManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault()?.Creature;
        return null;
    }

    /// <summary>持有者获得格挡时乘以 0.8，即减少 20%。</summary>
    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == Owner)
            return BlockMultiplier;
        return 1m;
    }

    /// <summary>执行击退：计算方向（远离施加者/默认向左）、保存原始位置、执行 Tween 动画。</summary>
    private async Task ApplyKnockback(Creature applier, int stacks)
    {
        if (applier == null || Owner == null) return;

        var node = NCombatRoom.Instance?.GetCreatureNode(Owner);
        var applierNode = NCombatRoom.Instance?.GetCreatureNode(applier);
        if (node == null) return;

        if (!_positionSaved)
        {
            _originalX = node.GlobalPosition.X;
            _positionSaved = true;
        }

        float currentKnockback = Math.Min(stacks * KnockbackPerStack, MaxTotalKnockback - _totalKnockback);
        if (currentKnockback <= 0) return;

        _totalKnockback += currentKnockback;

        bool isToLeft = applierNode != null
            ? node.GlobalPosition.X > applierNode.GlobalPosition.X
            : true;

        if (isToLeft)
            await MovementHelper.Knockback(Owner, currentKnockback);
        else
            await MovementHelper.MoveRight(Owner, currentKnockback);
    }

    /// <summary>首次施加恐惧时触发击退。</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount <= 0) return;
        var resolved = ResolveApplier(applier);
        if (resolved != null && resolved != Owner)
            await ApplyKnockback(resolved, (int)Amount);
    }

    /// <summary>恐惧层数变化时，按增量值追加击退距离。</summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,PowerModel power,decimal amount,Creature? applier,CardModel? cardSource)
    {
        if (power != this) return;
        if (amount <= 0) return;
        var resolved = ResolveApplier(applier);
        if (resolved != null && resolved != Owner)
            await ApplyKnockback(resolved, (int)amount);
    }

    /// <summary>回合结束时将持有者移回原位，清除全部恐惧层数。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext,CombatSide side,IEnumerable<Creature> participants)
    {
        await PowerCmd.Remove(this);
        if (!participants.Contains(Owner)) return;
        if (!_positionSaved) return;

        if (_totalKnockback > 0)
            await MovementHelper.MoveToX(Owner, _originalX);
    }
}
