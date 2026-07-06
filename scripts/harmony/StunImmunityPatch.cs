using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ArchiveLibrary.Scripts.Patches;

/// <summary>
/// 实现此接口的 Power 会让持有者免疫击晕，同时显示"免疫"文字特效。
/// </summary>
public interface IStunImmunePower { }

/// <summary>击晕免疫工具：支持 Power 免疫 + 按怪物 ID/类型免疫。</summary>
public static class StunImmunity
{
    /// <summary>在此注册的怪物 ID 将天生免疫击晕（如召唤物、特定Boss）。</summary>
    public static readonly HashSet<string> ImmuneMonsterIds = new();

    /// <summary>检查 creature 是否免疫击晕。</summary>
    public static bool IsImmune(Creature creature)
    {
        // Power 免疫
        if (creature.Powers.Any(p => p is IStunImmunePower))
            return true;
        // 怪物 ID 免疫
        if (creature.Monster?.Id != null && ImmuneMonsterIds.Contains(creature.Monster.Id.Entry))
            return true;
        return false;
    }
}

/// <summary>免疫击晕时拦截击晕并显示"免疫"文字。</summary>
[HarmonyPatch(typeof(Creature), "StunInternal")]
public static class StunImmunityPatch
{
    static bool Prefix(Creature __instance)
    {
        if (StunImmunity.IsImmune(__instance))
        {
            Visual.NImmuneVfx.Display(__instance, EntiyArchiveLibrary.UI.GetText("IMMUNE_STUN"));
            return false;
        }
        return true;
    }
}
