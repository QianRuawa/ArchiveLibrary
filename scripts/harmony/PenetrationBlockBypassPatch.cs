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

        var target = PenetrationBenefit.PendingTarget;
        var pen = PenetrationBenefit.PendingAmount;

        //LibraryLogger.Info($"[贯穿-DamageBlockInternal] PendingTarget={target?.GetHashCode()} PendingAmount={pen} target==instance={target == __instance}");

        if (target == __instance && pen > 0)
        {
            decimal bypass = Math.Min(pen, amount);
            decimal blockable = amount - bypass;
            decimal blocked = Math.Min(blockable, __instance.Block);

            __instance.LoseBlockInternal(blocked);
            __result = blocked;

            //LibraryLogger.Info($"[贯穿] 穿透! bypass={bypass} blockable={blockable} blocked={blocked} originalBlock={__instance.Block + blocked}");
            return false;
        }

       // LibraryLogger.Info($"[贯穿] 未穿透，正常执行 DamageBlockInternal");
        return true;
    }
}
