using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ArchiveLibrary.Scripts.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// EX消耗COST减少：持有者手牌费用减少。
///
/// 1模式 - 普通减半：对所有手牌向下取半，显示层数，打出卡牌后消耗一层。
/// 2模式 - 指定减少：对单张指定卡牌减少固定费用，不显示层数，不消耗。
///   未指定目标时自动按顺序分配到手牌（层数 % 手牌数）。
/// </summary>
[RegisterPower]
public class ExCostReductionBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "CostChange";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => _mode == ReduceMode.NormalHalf ? PowerStackType.Counter : PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>减少模式</summary>
    public enum ReduceMode
    {
        /// <summary>普通减半：对所有手牌向下取半</summary>
        NormalHalf,
        /// <summary>指定减少：对单张指定卡牌减少 Amount 点</summary>
        SpecificReduction
    }

    private ReduceMode _mode = ReduceMode.NormalHalf;

    public ReduceMode Mode
    {
        get => _mode;
        set { _mode = value; RefreshModeDisplay(); }
    }

    private CardModel? _targetCard;

    /// <summary>指定减少的目标卡牌（2模式用，为 null 时自动分配）</summary>
    public CardModel? TargetCard
    {
        get => _targetCard;
        set { _targetCard = value; RefreshModeDisplay(); }
    }

    /// <summary>1模式显示层数，2模式不显示</summary>
    public override int DisplayAmount => _mode == ReduceMode.NormalHalf ? Amount : 0;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        RefreshModeDisplay();
        return Task.CompletedTask;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("ModeDesc", UIHelper.Get("COST_REDUCTION_NORMAL_HALF"))
    ];

    /// <summary>刷新模式描述文字供 smartDescription 显示</summary>
    private void RefreshModeDisplay()
    {
        if (DynamicVars == null) return;
        if (DynamicVars["ModeDesc"] is not StringVar sv) return;

        sv.StringValue = _mode == ReduceMode.SpecificReduction && _targetCard != null
            ? string.Format(UIHelper.Get("COST_REDUCTION_SPECIFIC"), _targetCard.Title, Amount)
            : UIHelper.Get("COST_REDUCTION_NORMAL_HALF");

        InvokeDisplayAmountChanged();
    }

    /// <summary>是否已自动分配过目标</summary>
    private bool _autoAssigned;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner) return false;
        if (card.Pile?.Type != PileType.Hand) return false;
        if (originalCost < 1m) return false;

        int newCost;
        switch (_mode)
        {
            case ReduceMode.NormalHalf:
                newCost = (int)Math.Floor((double)originalCost / 2.0);
                break;

            case ReduceMode.SpecificReduction:
                // 未指定目标时自动分配
                if (TargetCard == null && !_autoAssigned && card.Owner != null)
                    AutoAssignTarget(card.Owner);

                // 仅对指定目标生效
                if (TargetCard != null && card != TargetCard)
                    return false;

                newCost = Math.Max(0, (int)originalCost - Amount);
                break;

            default:
                return false;
        }

        if (newCost < originalCost)
        {
            modifiedCost = newCost;
            return true;
        }
        return false;
    }

    /// <summary>自动分配目标卡牌：按层数顺序选择手牌</summary>
    private void AutoAssignTarget(Player player)
    {
        _autoAssigned = true;
        try
        {
            var hand = PileType.Hand.GetPile(player);
            if (hand.Cards.Count > 0)
            {
                int idx = (Amount - 1) % hand.Cards.Count;
                TargetCard = hand.Cards[idx];
            }
        }
        catch
        {
            // 非战斗状态下忽略
        }
    }

    /// <summary>打出卡牌后消耗一层（1模式任意卡牌，2模式仅被指定的卡牌）</summary>
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || Amount <= 0) return;

        if (_mode == ReduceMode.NormalHalf)
            await PowerCmd.Decrement(this);
        else if (_mode == ReduceMode.SpecificReduction && cardPlay.Card == TargetCard)
            await PowerCmd.Remove(this);
    }

    // ===== 静态 API（供其他 Mod 制作者使用）=====

    /// <summary>给予 NormalHalf 模式（全局费用减半）。</summary>
    public static Task ApplyNormalHalf(PlayerChoiceContext ctx, Creature target, int stacks)
    {
        var proto = ModelDb.DebugPower(typeof(ExCostReductionBenefit)).ToMutable();
        if (proto is ExCostReductionBenefit ex)
            ex.Mode = ReduceMode.NormalHalf;
        return PowerCmd.Apply(ctx, proto, target, stacks, null, null);
    }

    /// <summary>给予 SpecificReduction 模式（指定卡牌减费）。</summary>
    public static Task ApplySpecificReduction(PlayerChoiceContext ctx, Creature target, CardModel targetCard, int reduceAmount)
    {
        var proto = ModelDb.DebugPower(typeof(ExCostReductionBenefit)).ToMutable();
        if (proto is ExCostReductionBenefit ex)
        {
            ex.Mode = ReduceMode.SpecificReduction;
            ex.TargetCard = targetCard;
        }
        return PowerCmd.Apply(ctx, proto, target, reduceAmount, null, null);
    }
}
