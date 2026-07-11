using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ArchiveLibrary.Scripts.Powers;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.ConsoleCommands;

/// <summary>
/// 档案库智能 buff 给予命令。
/// </summary>
public class AlBuffConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "albuff";
    public override string Args => "<powerId> [args...]";
    public override string Description => "档案库智能buff给予";
    public override bool IsNetworked => true;
    public override bool DebugOnly => false;

    private static List<Type>? _powerTypes;
    private static readonly Dictionary<Type, string> _powerTitleCache = new();

    private static void EnsureLoaded()
    {
        if (_powerTypes != null) return;
        _powerTypes = ModelDb.AllAbstractModelSubtypes
            .Where(t => t.IsSubclassOf(typeof(PowerModel))
                     && t.Namespace == "ArchiveLibrary.Scripts.Powers")
            .ToList();
    }

    private static string GetPowerTitle(Type t)
    {
        if (_powerTitleCache.TryGetValue(t, out var cached)) return cached;
        try
        {
            var title = ModelDb.DebugPower(t).Title.GetFormattedText();
            _powerTitleCache[t] = title;
            return title;
        }
        catch
        {
            return t.Name;
        }
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        EnsureLoaded();
        if (args.Length < 2)
            return Usage();

        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null)
            return new CmdResult(false, "不在战斗中");

        var powerType = FindPowerType(args[0]);
        if (powerType == null)
            return new CmdResult(false, $"未找到能力: {args[0]}\n可用: {string.Join(", ", _powerTypes!.Select(t => GetPowerTitle(t)))}");

        var creatures = combatState.Creatures;
        string name = powerType.Name;
        string displayName = GetPowerTitle(powerType);
        var choiceContext = new BlockingPlayerChoiceContext();

        // 解析目标索引(默认 0 = 玩家自己)
        int? parseTargetIdx(int idx)
        {
            if (idx >= args.Length) return 0; // 缺省目标为玩家
            if (!int.TryParse(args[idx], out var t)) return null;
            if (t < 0 || t >= creatures.Count) return null;
            return t;
        }

        try
        {
            // 1. _MODIFIER 类型：Buff/Debuff + 量 + [目标]
            if (name.EndsWith("Modifier"))
            {
                if (args.Length < 3)
                    return new CmdResult(false, $"用法: albuff {args[0]} Buff|Debuff <量> [目标]");

                int sign = args[1].Equals("Buff", StringComparison.OrdinalIgnoreCase) ? 1 :
                           args[1].Equals("Debuff", StringComparison.OrdinalIgnoreCase) ? -1 :
                           throw new ArgumentException($"方向请用 Buff 或 Debuff");
                int amount = int.Parse(args[2]);
                var targetIdx = parseTargetIdx(3);
                if (targetIdx == null) return BadTarget(creatures.Count);

                int finalAmount = sign * amount;
                var task = PowerCmd.Apply(choiceContext, ModelDb.DebugPower(powerType).ToMutable(), creatures[targetIdx.Value], finalAmount, null, null);
                return new CmdResult(task, true, $"已给予 {displayName} (Amount={finalAmount}) 给 目标{targetIdx.Value}");
            }

            // 2. 特殊：受击回复
            if (name == "HealByHitBenefit")
            {
                if (args.Length < 3)
                    return new CmdResult(false, $"用法: albuff {args[0]} <消耗层> <层数> [目标]");

                int consumable = int.Parse(args[1]);
                int healAmount = int.Parse(args[2]);
                var targetIdx = parseTargetIdx(3);
                if (targetIdx == null) return BadTarget(creatures.Count);

                var target = creatures[targetIdx.Value];
                var proto = ModelDb.DebugPower(powerType).ToMutable();
                // 先以消耗层数 Apply（AfterApplied 会设 _charges = Amount = consumable）
                var applyTask = PowerCmd.Apply(choiceContext, proto, target, consumable, null, null);
                // 再调整 Amount 到实际治疗量
                var fullTask = applyTask.ContinueWith(async _ =>
                {
                    if (healAmount != consumable)
                    {
                        var p = target.GetPower<HealByHitBenefit>();
                        if (p != null)
                            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), p, healAmount - consumable, null, null);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
                return new CmdResult(fullTask, true, $"已给予 {displayName} (消耗层={consumable}, 层数={healAmount}) 给 目标{targetIdx.Value}");
            }

            // 特殊：费用减少
            if (name == "ExCostReductionBenefit")
            {
                if (args.Length < 3)
                    return new CmdResult(false, $"用法: albuff {args[0]} NormalHalf|SpecificReduction [参数...] [目标]");

                string modeStr = args[1];

                if (modeStr.Equals("NormalHalf", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 3)
                        return new CmdResult(false, $"用法: albuff {args[0]} NormalHalf <层数> [目标]");
                    int amount = int.Parse(args[2]);
                    var targetIdx = parseTargetIdx(3);
                    if (targetIdx == null) return BadTarget(creatures.Count);

                    var target = creatures[targetIdx.Value];
                    // 找现有 NormalHalf 实例叠层，不干扰 SpecificReduction
                    var existing = target.Powers.OfType<ExCostReductionBenefit>()
                        .FirstOrDefault(p => p.Mode == ExCostReductionBenefit.ReduceMode.NormalHalf);
                    if (existing != null)
                    {
                        var task = PowerCmd.ModifyAmount(choiceContext, existing, amount, null, null);
                        return new CmdResult(task, true, $"已给予 {displayName} NormalHalf (+{amount}, 总计{existing.Amount + amount}) 给 目标{targetIdx.Value}");
                    }
                    var powerProto = ModelDb.DebugPower(powerType).ToMutable();
                    if (powerProto is ExCostReductionBenefit nh)
                        nh.Mode = ExCostReductionBenefit.ReduceMode.NormalHalf;
                    var applyTask = PowerCmd.Apply(choiceContext, powerProto, target, amount, null, null);
                    return new CmdResult(applyTask, true, $"已给予 {displayName} NormalHalf (层数={amount}) 给 目标{targetIdx.Value}");
                }
                else if (modeStr.Equals("SpecificReduction", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 4)
                        return new CmdResult(false, $"用法: albuff {args[0]} SpecificReduction <手牌索引> <减少费用> [目标]");

                    int handIdx = int.Parse(args[2]);
                    int reduceAmount = int.Parse(args[3]);
                    var targetIdx = parseTargetIdx(4);
                    if (targetIdx == null) return BadTarget(creatures.Count);

                    var hand = PileType.Hand.GetPile(issuingPlayer!);
                    if (handIdx < 0 || handIdx >= hand.Cards.Count)
                        return new CmdResult(false, $"手牌索引 {handIdx} 超出范围 [0, {hand.Cards.Count - 1}]");

                    var target = creatures[targetIdx.Value];
                    var targetCard = hand.Cards[handIdx];
                    var powerProto = ModelDb.DebugPower(powerType).ToMutable();
                    if (powerProto is ExCostReductionBenefit sr)
                    {
                        sr.Mode = ExCostReductionBenefit.ReduceMode.SpecificReduction;
                        sr.TargetCard = targetCard;
                    }
                    var task = PowerCmd.Apply(choiceContext, powerProto, target, reduceAmount, null, null);
                    return new CmdResult(task, true, $"已给予 {displayName} SpecificReduction (目标卡牌: {targetCard.Title}, 减少{reduceAmount}费) 给 目标{targetIdx.Value}");
                }
                return new CmdResult(false, "模式请用 NormalHalf 或 SpecificReduction");
            }

            // 特殊：伤害免疫
            if (name == "ImmuneDamageParticular")
            {
                if (args.Length < 3)
                    return new CmdResult(false, $"用法: albuff {args[0]} Card|NonCard [目标]");

                bool blockNonCard = args[1].Equals("NonCard", StringComparison.OrdinalIgnoreCase);
                var targetIdx = parseTargetIdx(2);
                if (targetIdx == null) return BadTarget(creatures.Count);

                var target = creatures[targetIdx.Value];
                var task = ImmuneDamageParticular.Acquire(new BlockingPlayerChoiceContext(), target, blockNonCard);
                return new CmdResult(task, true, $"已给予 {displayName} (模式={(blockNonCard ? "非卡牌" : "卡牌")}) 给 目标{targetIdx.Value}");
            }

            // 3. 普通类型：层数 + [目标]
            {
                int amount = int.Parse(args[1]);
                var targetIdx = parseTargetIdx(2);
                if (targetIdx == null) return BadTarget(creatures.Count);

                var task = PowerCmd.Apply(choiceContext, ModelDb.DebugPower(powerType).ToMutable(), creatures[targetIdx.Value], amount, null, null);
                return new CmdResult(task, true, $"已给予 {displayName} (Amount={amount}) 给 目标{targetIdx.Value}");
            }
        }
        catch (Exception ex)
        {
            LibraryLogger.Error($"albuff 执行失败", ex);
            return new CmdResult(false, $"执行失败: {ex.Message}");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        int argc = args.Length;

        // 第1参数：能力ID
        if (argc <= 1)
        {
            EnsureLoaded();
            var names = _powerTypes!.Select(t => $"{t.Name}({GetPowerTitle(t)})").ToList();
            return CompleteArgument(names, Array.Empty<string>(), args.FirstOrDefault() ?? "");
        }

        // 第2参数：根据能力类型提供可选值
        if (argc == 2)
        {
            var powerType = FindPowerType(args[0]);
            if (powerType != null)
            {
                string name = powerType.Name;
                if (name.EndsWith("Modifier"))
                    return CompleteArgument(new[] { "Buff", "Debuff" }, Array.Empty<string>(), args[1]);

                if (name == "ExCostReductionBenefit")
                    return CompleteArgument(new[] { "NormalHalf", "SpecificReduction" }, Array.Empty<string>(), args[1]);

                if (name == "ImmuneDamageParticular")
                    return CompleteArgument(new[] { "Card", "NonCard" }, Array.Empty<string>(), args[1]);
            }
            return base.GetArgumentCompletions(player, args);
        }

        // 手牌索引补全(ExCostReductionBenefit SpecificReduction 第3参数)
        if (argc == 3 && args[0].Equals("ExCostReductionBenefit", StringComparison.OrdinalIgnoreCase)
            && args[1].Equals("SpecificReduction", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var hand = PileType.Hand.GetPile(player!);
                if (hand.Cards.Count > 0)
                {
                    var cards = hand.Cards.Select((c, i) => $"{i}({c.Title})").ToList();
                    return CompleteArgument(cards, Array.Empty<string>(), args[2]);
                }
            }
            catch { }
        }

        return base.GetArgumentCompletions(player, args);
    }

    // ===== 辅助方法 =====

    private static Type? FindPowerType(string id)
    {
        int idx = id.IndexOf('(');
        if (idx > 0) id = id[..idx];
        return _powerTypes?.FirstOrDefault(t =>
            t.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private static CmdResult BadTarget(int max) =>
        new CmdResult(false, $"目标索引无效，范围 [0, {max - 1}](0=玩家自己)");

    private static CmdResult Usage()
    {
        return new CmdResult(false,
            "用法:\n" +
            "  albuff <能力ID> Buff|Debuff <量> [目标]            — Modifier 双生\n" +
            "  albuff <能力ID> <层数> [目标]                       — 普通能力\n" +
            "  albuff HEAL_BY_HIT_BENEFIT <消耗> <层> [目标]        — 受击回复\n" +
            "  albuff EX_COST_REDUCTION_BENEFIT NormalHalf <层> [目标] — EX消耗COST减少\n" +
            "  albuff EX_COST_REDUCTION_BENEFIT SpecificReduction <手牌Idx> <减少> [目标]\n" +
            "  albuff IMMUNE_DAMAGE_PARTICULAR Card|NonCard [目标]     — 伤害免疫\n" +
            "  [目标] 缺省为 0(玩家自己)");
    }
}
