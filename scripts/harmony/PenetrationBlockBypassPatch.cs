using ArchiveLibrary.Scripts.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 防御贯穿 — 格挡穿透层：
/// 每 1 点贯穿允许 1 点伤害绕过格挡，不消耗这部分格挡。
/// 剩余伤害正常扣除格挡。
/// </summary>
[HarmonyPatch(typeof(Creature), "DamageBlockInternal")]
public static class PenetrationBlockBypassPatch
{
    public static bool Prefix(Creature __instance, decimal amount, ValueProp props, ref decimal __result)
    {
        //LibraryLogger.Info($"[贯穿-DamageBlockInternal] target={__instance.GetHashCode()} Block={__instance.Block} damage={amount} props={props}");

        if (props.HasFlag(ValueProp.Unblockable))
            return true;

        // 只有攻击伤害才能触发贯穿（过滤毒/妄想吃等非攻击伤害）
        if (!props.IsPoweredAttack())
            return true;

        // 优先用 PenetrationBenefit 的穿透，再用外部穿透（如穿甲弹）
        var target = PenetrationBenefit.PendingTarget;
        var pen = PenetrationBenefit.PendingAmount;

        if (!(target == __instance && pen > 0))
        {
            target = PenetrationHelper.ApTarget;
            pen = PenetrationHelper.ApAmount;
            PenetrationHelper.Clear();
        }

        //LibraryLogger.Info($"[贯穿] 目标={__instance.GetHashCode()} Block={__instance.Block} 伤害={amount} 穿透目标={target?.GetHashCode()} 穿透量={pen}");

        if (target == __instance && pen > 0)
        {
            decimal bypass = Math.Min(pen, amount);
            decimal blockable = amount - bypass;
            decimal blocked = Math.Min(blockable, __instance.Block);

            __instance.LoseBlockInternal(blocked);
            __result = blocked;

            //LibraryLogger.Info($"[贯穿] 穿透! bypass={bypass} blockable={blockable} blocked={blocked}");
            return false;
        }

        return true;
    }
}
