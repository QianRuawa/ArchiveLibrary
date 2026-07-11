using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.Combat;

/// <summary>
/// 临时卡牌伤害提升工具。
/// 为指定卡牌临时增加伤害，回合结束时自动恢复。
/// 适用于"下一张牌伤害+X"类效果。
/// </summary>
public static class TempDamageBoost
{
	private static readonly Dictionary<CardModel, decimal> _boosted = new();

	/// <summary>为卡牌临时增加伤害（直接修改 DynamicVars.Damage.BaseValue）。</summary>
	public static void Add(CardModel card, decimal amount)
	{
		if (card == null || amount == 0) return;
		if (card.DynamicVars?.Damage == null) return;

		if (_boosted.TryGetValue(card, out var existing))
			_boosted[card] = existing + amount;
		else
			_boosted[card] = amount;

		card.DynamicVars.Damage.BaseValue += amount;
	}

	/// <summary>手动清除所有临时提升（通常在 Harmony 补丁触发，也可手动调用）。</summary>
	public static void ClearAll()
	{
		foreach (var kvp in _boosted)
		{
			if (kvp.Key?.DynamicVars?.Damage != null)
				kvp.Key.DynamicVars.Damage.BaseValue -= kvp.Value;
		}
		_boosted.Clear();
	}
}

/// <summary>回合结束时自动清除临时伤害加成。</summary>
[HarmonyPatch(typeof(PlayerCombatState), "EndOfTurnCleanup")]
internal static class TempDamageBoostCleanupPatch
{
	static void Postfix() => TempDamageBoost.ClearAll();
}
