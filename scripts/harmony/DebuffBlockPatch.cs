using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ArchiveLibrary.Scripts.Utils;
using ArchiveLibrary.Scripts.Visual;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 拦截 <see cref="PowerCmd.Apply"/>，<see cref="AllImmuneParticular"/> 持有者免疫所有施加的状态。
/// 但允许什亭之匣自身被施加（否则无法给予）。
/// </summary>
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply),
    typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal),
    typeof(Creature), typeof(CardModel), typeof(bool))]
public static class DebuffBlockPatch
{
    public static void Prefix(PowerModel power, Creature target, ref decimal amount)
    {
        if (target == null || power == null) return;
        if (power is AllImmuneParticular) return; // 允许自己施加
        if (target.GetPower<AllImmuneParticular>() == null) return;

        LibraryLogger.Debug($"什亭之匣：{target} 免疫状态 [{power.Id.Entry}]");
        NImmuneVfx.Display(target);
        amount = 0m;
    }
}

/// <summary>
/// 什亭之匣无法被移除。
/// </summary>
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Remove), typeof(PowerModel))]
public static class AllImmuneRemovePatch
{
    public static bool Prefix(PowerModel power)
    {
        if (power is AllImmuneParticular)
        {
            if (AllImmuneParticular.AllowRemoveOnce)
            {
                AllImmuneParticular.AllowRemoveOnce = false;
                LibraryLogger.Info($"老师...");
                return true;
            }
            LibraryLogger.Info($"⚠什亭之匣：拒绝移除⚠");
            return false;
        }
        return true;
    }
}
