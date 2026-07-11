using System.Reflection;
using ArchiveLibrary.Scripts.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;

namespace ArchiveLibrary.Scripts.Act;

/// <summary>
/// 通用自定义 Boss 注册和注入系统。
/// 模组在 Init 中注册每个 Act 的自定义 Boss，系统自动通过 Harmony 补丁注入。
/// </summary>
public static class CustomBossRegistry
{
	internal static readonly List<ActBossEntry> _entries = new();

	/// <summary>注册一个 Act 的自定义 Boss 列表。</summary>
	public static void RegisterActBosses(Type actType, Func<bool> enabled, Func<bool> fixed100Pct, params Type[] bossTypes)
	{
		if (actType == null || bossTypes == null || bossTypes.Length == 0) return;
		_entries.RemoveAll(e => e.ActType == actType);
		_entries.Add(new ActBossEntry
		{
			ActType = actType,
			Enabled = enabled,
			Fixed100Pct = fixed100Pct,
			BossTypes = bossTypes
		});
		var names = string.Join(", ", bossTypes.Select(t => t.Name));
		LibraryLogger.Info($"[CustomBoss] 注册 Act={actType.Name} Boss=[{names}]");

	}

	/// <summary>泛型版本。</summary>
	public static void RegisterActBosses<TAct>(Func<bool> enabled, Func<bool> fixed100Pct, params Type[] bossTypes)
		where TAct : ActModel
		=> RegisterActBosses(typeof(TAct), enabled, fixed100Pct, bossTypes);

	/// <summary>获取指定 Act 的自定义 Boss 条目。</summary>
	public static ActBossEntry? GetEntry(Type actType)
		=> _entries.FirstOrDefault(e => e.ActType == actType);

	/// <summary>判断指定的 Encounter 是否为已注册的自定义 Boss。</summary>
	public static bool IsCustomBoss(EncounterModel encounter)
	{
		var encounterType = encounter.GetType();
		return _entries.Any(e => e.BossTypes.Contains(encounterType));
	}

	/// <summary>判断指定 Encounter 在当前设置下是否可在运行时出现。</summary>
	public static bool IsBossEnabledForEncounter(EncounterModel encounter)
	{
		var entry = _entries.FirstOrDefault(e => e.BossTypes.Contains(encounter.GetType()));
		return entry == null || entry.Enabled();
	}

	/// <summary>
	/// 注册完毕后调用，为每个已注册的 Act 手动应用 Harmony 补丁。
	/// 不依赖 [HarmonyPatch] 属性，避免 PatchAll 时 _entries 为空报错。
	/// </summary>
	public static void ApplyPatches()
	{
		var harmony = new Harmony("archive-library.custom-boss");
		var genPostfix = new HarmonyMethod(typeof(Patches), nameof(Patches.GeneratePostfix));
		var orderPostfix = new HarmonyMethod(typeof(Patches), nameof(Patches.OrderPostfix));
		int count = 0;

		foreach (var entry in _entries)
		{
			var genMethod = entry.ActType.GetMethod("GenerateAllEncounters", Type.EmptyTypes);
			if (genMethod != null)
			{
				harmony.Patch(genMethod, postfix: genPostfix);
				count++;
			}

			var orderProp = entry.ActType.GetProperty("BossDiscoveryOrder");
			if (orderProp?.GetMethod != null)
			{
				harmony.Patch(orderProp.GetMethod, postfix: orderPostfix);
				count++;
			}
		}
		LibraryLogger.Info($"[CustomBoss] 已应用 {count} 个补丁，共 {_entries.Count} 个 Act");
	}

	public class ActBossEntry
	{
		public Type ActType;
		public Func<bool> Enabled;
		public Func<bool> Fixed100Pct;
		public Type[] BossTypes;
	}
}

// ==================== 补丁方法（无 [HarmonyPatch] 属性，由 ApplyPatches 手动注册） ====================

internal static class Patches
{
	internal static void GeneratePostfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
	{
		var entry = CustomBossRegistry.GetEntry(__instance.GetType());
		if (entry == null) return;

		// 始终添加自定义 Boss（AllEncounters 缓存供图鉴 + 运行使用）
		var list = __result.ToList();
		foreach (var bossType in entry.BossTypes)
		{
			if (!list.Any(e => e.GetType() == bossType))
			{
				var bossEncounter = ModelDbHelper.GetEncounter(bossType);
				if (bossEncounter != null)
					list.Add(bossEncounter);
			}
		}
		__result = list;

		LibraryLogger.Info($"[CustomBoss] {__instance.GetType().Name} AllEncounters += {entry.BossTypes.Length} 个自定义 Boss");

	}

	internal static void OrderPostfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
	{
		var entry = CustomBossRegistry.GetEntry(__instance.GetType());
		if (entry == null) return;

		bool enabled = entry.Enabled();
		bool fixedPct = entry.Fixed100Pct();
		if (!enabled) return;

		var list = __result.ToList();
		foreach (var bossType in entry.BossTypes)
		{
			if (!list.Any(e => e.GetType() == bossType))
			{
				var bossEncounter = ModelDbHelper.GetEncounter(bossType);
				if (bossEncounter != null)
					list.Add(bossEncounter);
			}
		}

		LibraryLogger.Info($"[CustomBoss] {__instance.GetType().Name}.BossDiscoveryOrder 注入完成, 共 {list.Count} 个 Boss");

		if (fixedPct)
			list.RemoveAll(e => e.RoomType == RoomType.Boss && !CustomBossRegistry.IsCustomBoss(e));

		__result = list;
	}
}

/// <summary>修正自定义 Act 的多人 HP 缩放（原版只支持索引 0-2）。</summary>
[HarmonyPatch(typeof(MultiplayerScalingModel), "GetMultiplayerScaling")]
internal static class ActMultiplayerScalingPatch
{
	static bool Prefix(EncounterModel? encounter, int actIndex, ref decimal __result)
	{
		if (actIndex < 3) return true; // 原版逻辑
		// 自定义 Act 使用 Act 3 (index 2) 的缩放
		__result = encounter?.RoomType == RoomType.Boss ? 1.3m : 1.2m;
		return false;
	}
}

/// <summary>通过反射调用 ModelDb.Encounter&lt;T&gt;()。</summary>
internal static class ModelDbHelper
{
	private static readonly MethodInfo _encounterMethod = typeof(ModelDb).GetMethod("Encounter", Type.EmptyTypes);

	public static EncounterModel? GetEncounter(Type bossType)
	{
		var generic = _encounterMethod?.MakeGenericMethod(bossType);
		return generic?.Invoke(null, null) as EncounterModel;
	}
}

