using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ArchiveLibrary.Scripts.Powers;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 拦截治疗，应用 <see cref="HealLimitAdverse"/> 的回复上限。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal), typeof(bool))]
public static class HealLimitPatch
{
    public static void Prefix(Creature creature, ref decimal amount)
    {
        if (creature == null || amount <= 0m) return;

        var limit = creature.GetPower<HealLimitAdverse>();
        if (limit == null) return;

        if (amount > limit.Amount)
        {
            LibraryLogger.Debug($"治疗限制: {amount} → {limit.Amount}");
            amount = limit.Amount;
        }
    }
}
