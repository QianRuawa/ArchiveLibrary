using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 穿透辅助通道。供外部（如穿甲弹）设置临时穿透值，
/// PenetrationBlockBypassPatch 会自动检测并处理。
/// 不干扰 PenetrationBenefit 的正常穿透逻辑。
/// </summary>
public static class PenetrationHelper
{
	/// <summary>外部穿透目标。</summary>
	internal static Creature? ApTarget;
	/// <summary>外部穿透量。</summary>
	internal static decimal ApAmount;

	/// <summary>穿甲弹设置穿透。</summary>
	public static void SetArmorPiercing(Creature target, decimal amount)
	{
		ApTarget = target;
		ApAmount = amount;
	}

	/// <summary>清空。</summary>
	public static void Clear()
	{
		ApTarget = null;
		ApAmount = 0;
	}
}
