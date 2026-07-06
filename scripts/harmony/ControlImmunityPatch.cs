using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using ArchiveLibrary.Scripts.Utils;
using ArchiveLibrary.Scripts.Visual;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 拦截击晕和强制结束回合，<see cref="ControlImmunityParticular"/> 持有者免疫。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Stun), typeof(Creature), typeof(string))]
public static class StunImmunityPatch
{
    public static bool Prefix(Creature creature)
    {
        if (creature == null) return true;
        if (creature.GetPower<ControlImmunityParticular>() != null)
        {
            LibraryLogger.Debug($"控制免疫：{creature} 免疫击晕");
            NImmuneVfx.Display(creature, EntiyArchiveLibrary.UI.GetText("IMMUNE_NEGATIVE"));
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn), typeof(Player), typeof(bool), typeof(Func<Task>))]
public static class EndTurnImmunityPatch
{
    public static bool Prefix(Player player, bool canBackOut)
    {
        if (player == null) return true;
        if (!canBackOut && player.Creature?.GetPower<ControlImmunityParticular>() != null)
        {
            LibraryLogger.Debug($"控制免疫：{player} 免疫强制结束回合");
            if (player.Creature != null) NImmuneVfx.Display(player.Creature, EntiyArchiveLibrary.UI.GetText("IMMUNE_NEGATIVE"));
            return false;
        }
        return true;
    }
}
