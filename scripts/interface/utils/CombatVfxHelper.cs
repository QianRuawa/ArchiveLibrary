using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using HarmonyLib;
using ArchiveLibrary.Scripts.Utils;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ArchiveLibrary.Scripts.Combat;

/// <summary>
/// 战斗 VFX 辅助工具：获取玩家朝向、摄像机缩放等。
/// 为制作自定义 VFX 的模组制作者提供便利。
/// </summary>
public static class CombatVfxHelper
{
	/// <summary>检测玩家是否面朝左。</summary>
	public static bool IsPlayerFacingLeft(Creature creature)
	{
		if (creature?.Player == null) return false;
		try
		{
			var surrounded = creature.GetPower<SurroundedPower>();
			if (surrounded != null)
				return surrounded.Facing == SurroundedPower.Direction.Left;

			var node = NCombatRoom.Instance?.GetCreatureNode(creature);
			if (node?.Body != null)
				return node.Body.Scale.X < 0;
		}
		catch { }
		return false;
	}

	/// <summary>
	/// 获取当前战斗的摄像机缩放值（基于 SceneContainer.Scale）。
	/// 用于补偿 VFX 在 UI 层渲染时不受摄像机缩放影响的大小。
	/// </summary>
	public static float GetCameraZoom()
	{
		try
		{
			if (NCombatRoom.Instance?.SceneContainer != null)
				return NCombatRoom.Instance.SceneContainer.Scale.X;
		}
		catch { }
		return 1f;
	}

	/// <summary>获取指定 creature 所在视口的摄像机缩放。</summary>
	public static float GetCameraZoomFromCreature(Creature creature)
	{
		return GetCameraZoom();
	}
}

/// <summary>每个战斗房间初始化时输出摄像机缩放值。</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class CombatRoomZoomLogPatch
{
	static void Postfix(NCombatRoom __instance)
	{
		try
		{
			var cam = __instance.GetViewport()?.GetCamera2D();
			float zoom = cam?.Zoom.X ?? 1f;
			//LibraryLogger.Info($"[CombatVfx] 战斗房间缩放={zoom:F3} 房间={__instance.Name}");
		}
		catch (Exception ex)
		{
			LibraryLogger.Error($"[CombatVfx] 获取缩放失败: {ex.Message}");
		}
	}
}
