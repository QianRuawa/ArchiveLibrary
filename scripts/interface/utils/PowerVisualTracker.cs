using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 通用 Power 视觉追踪器。
/// 当指定类型的 Power 被施加/移除时，自动显示/隐藏对应的视觉元素。
/// 在角色类的静态构造器中注册即可：
///
/// 用法（在 MiyuCharacter 的 static 构造器里）：
///   PowerVisualTracker.Register&lt;HeartPower&gt;(markerPath: "Visuals/HeartPos");
///   PowerVisualTracker.RegisterMarker&lt;HopePower&gt;(p => ((HopePower)p).MarkerName);
/// </summary>
public static class PowerVisualTracker
{
	private static readonly List<IconEntry> _icons = new();
	private static readonly List<MarkerEntry> _markers = new();

	/// <summary>注册图标型 Power。VFX 场景默认使用档案库的 vfx_heart.tscn。</summary>
	public static void Register<T>(string? vfxScene = null, string markerPath = "Visuals/HeartPos") where T : PowerModel
	{
		_icons.Add(new IconEntry { PowerType = typeof(T), VfxScene = vfxScene ?? "res://scene/vfx/vfx_heart.tscn", MarkerPath = markerPath });
	}

	/// <summary>注册标记型 Power：施加时显示对应的标记节点（Node2D / Node3D 均可）。</summary>
	public static void RegisterMarker<T>(Func<PowerModel, string> markerNameResolver) where T : PowerModel
	{
		_markers.Add(new MarkerEntry { PowerType = typeof(T), MarkerNameResolver = markerNameResolver });
	}

	internal class IconEntry { public Type PowerType; public string VfxScene; public string MarkerPath; }
	internal class MarkerEntry { public Type PowerType; public Func<PowerModel, string> MarkerNameResolver; }

	internal static IReadOnlyList<IconEntry> Icons => _icons;
	internal static IReadOnlyList<MarkerEntry> Markers => _markers;

	/// <summary>Godot 4 辅助：设置 Node 可见性（Node 基类无 Visible，需向下转换）。</summary>
	internal static void SetNodeVisible(Node node, bool visible)
	{
		if (node is CanvasItem ci) ci.Visible = visible;
		else if (node is Node3D n3d) n3d.Visible = visible;
	}
}

/// <summary>在角色视觉就绪时注入图标/标记监听逻辑。</summary>
[HarmonyPatch]
internal static class PowerVisualTrackerPatch
{
	static MethodBase TargetMethod() => typeof(NCreatureVisuals).GetMethod("_Ready", BindingFlags.Public | BindingFlags.Instance);

	static void Postfix(NCreatureVisuals __instance)
	{
		if (PowerVisualTracker.Icons.Count == 0 && PowerVisualTracker.Markers.Count == 0) return;
		if (!(__instance.GetParent() is NCreature nCreature)) return;
		var creature = nCreature.Entity;
		if (creature == null) return;

		var visuals = __instance.GetNodeOrNull<Node>("Visuals") ?? __instance;

		// ---- 初始化图标 ----
		var iconNodes = new Dictionary<Type, Node>();
		LibraryLogger.Info($"[PowerVisualTracker] 图标初始化开始, creature={creature.ModelId?.Entry}");
		foreach (var entry in PowerVisualTracker.Icons)
		{
			LibraryLogger.Info($"[PowerVisualTracker] 加载图标场景: {entry.VfxScene}");
			var scene = ResourceLoader.Load<PackedScene>(entry.VfxScene);
			if (scene == null) { LibraryLogger.Error($"[PowerVisualTracker] 场景加载失败: {entry.VfxScene}"); continue; }
			var marker = __instance.GetNodeOrNull<Node>(entry.MarkerPath) ?? __instance;
			LibraryLogger.Info($"[PowerVisualTracker] 标记节点 '{entry.MarkerPath}' 结果: {marker.Name}");
			var icon = scene.Instantiate<Node>();
			icon.Name = $"PowerIcon_{entry.PowerType.Name}";
			marker.AddChild(icon);
			iconNodes[entry.PowerType] = icon;
			bool hasPower = creature.Powers.Any(p => entry.PowerType.IsInstanceOfType(p));
			PowerVisualTracker.SetNodeVisible(icon, hasPower);
			LibraryLogger.Info($"[PowerVisualTracker] 图标已添加, 初始可见={hasPower}, 节点类型={icon.GetType().Name}, 父级={marker.GetPath()}");
		}

		// ---- 初始化标记节点缓存 ----
		var markerNodes = new Dictionary<(Type, string), Node>();
		foreach (var entry in PowerVisualTracker.Markers)
		{
			for (int i = 1; i <= 5; i++)
			{
				string name = $"Marker_{i}";
				var node = FindChildNode(__instance, name);
				if (node != null)
					markerNodes[(entry.PowerType, name)] = node;
			}
		}
		// 初始可见性
		foreach (var entry in PowerVisualTracker.Markers)
		{
			foreach (var kvp in markerNodes)
			{
				if (kvp.Key.Item1 != entry.PowerType) continue;
				bool active = creature.Powers.Any(p =>
					entry.PowerType.IsInstanceOfType(p) &&
					entry.MarkerNameResolver(p) == kvp.Key.Item2);
				PowerVisualTracker.SetNodeVisible(kvp.Value, active);
			}
		}

		// ---- 事件订阅 ----
		void OnApplied(PowerModel power)
		{
			if (!visuals.IsInsideTree()) { Cleanup(); return; }

			foreach (var e in PowerVisualTracker.Icons)
			{
				if (!e.PowerType.IsInstanceOfType(power)) continue;
				if (iconNodes.TryGetValue(e.PowerType, out var icon))
					PowerVisualTracker.SetNodeVisible(icon, true);
			}
			foreach (var e in PowerVisualTracker.Markers)
			{
				if (!e.PowerType.IsInstanceOfType(power)) continue;
				string mName = e.MarkerNameResolver(power);
				if (markerNodes.TryGetValue((e.PowerType, mName), out var node))
					PowerVisualTracker.SetNodeVisible(node, true);
			}
		}

		void OnRemoved(PowerModel power)
		{
			if (!visuals.IsInsideTree()) { Cleanup(); return; }

			foreach (var e in PowerVisualTracker.Icons)
			{
				if (!e.PowerType.IsInstanceOfType(power)) continue;
				if (!creature.Powers.Any(p => e.PowerType.IsInstanceOfType(p) && p != power))
				{
					if (iconNodes.TryGetValue(e.PowerType, out var icon))
						PowerVisualTracker.SetNodeVisible(icon, false);
				}
			}
			foreach (var e in PowerVisualTracker.Markers)
			{
				if (!e.PowerType.IsInstanceOfType(power)) continue;
				string mName = e.MarkerNameResolver(power);
				if (markerNodes.TryGetValue((e.PowerType, mName), out var node))
				{
					bool other = creature.Powers.Any(p =>
						e.PowerType.IsInstanceOfType(p) && p != power &&
						e.MarkerNameResolver(p) == mName);
					if (!other) PowerVisualTracker.SetNodeVisible(node, false);
				}
			}
		}

		void Cleanup()
		{
			creature.PowerApplied -= OnApplied;
			creature.PowerRemoved -= OnRemoved;
		}

		creature.PowerApplied += OnApplied;
		creature.PowerRemoved += OnRemoved;
		visuals.TreeExiting += Cleanup;
	}

	private static Node? FindChildNode(Node node, string name)
	{
		if (node.Name == name) return node;
		foreach (var child in node.GetChildren())
		{
			var r = FindChildNode(child, name);
			if (r != null) return r;
		}
		return null;
	}
}
