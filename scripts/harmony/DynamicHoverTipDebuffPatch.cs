using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 通用补丁：任何实现了 <see cref="IDynamicIconPower"/> 的 Power 在 Amount 为负时，
/// 自动将悬浮提示框标记为 Debuff（红色边框/背景），
/// Amount 为正时保持原样（蓝色增益框）。
/// </summary>
[HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
public static class DynamicHoverTipDebuffPatch
{
    public static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is not IDynamicIconPower)
            return;
        if (__instance.Amount >= 0)
            return;

        if (__result is List<IHoverTip> list && list.Count > 0 && list[0] is HoverTip tip)
        {
            list[0] = tip with { IsDebuff = true };
        }
    }
}
