using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.addons.mega_text;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 暴击时修改伤害数字颜色并添加前缀文字。
/// Label 不支持富文本，只能改颜色。
/// 注意：_Ready() 会在 AddChild 后重设 Label 文本，
/// 所以要用 Traverse 改 _text 私有字段让 _Ready() 读到修改后的值。
/// </summary>
[HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create), typeof(Creature), typeof(DamageResult))]
public static class CritDamageNumberPatch
{
    public static void Postfix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
    {
        // 查找所有 Creature 中是否有 CriticalChanceBenefit 标记了暴击
        var critPower = FindCritPower(target);
        LibraryLogger.Debug($"伤害数字补丁触发: __result={__result != null}, 有暴击={critPower != null}, 伤害={result.UnblockedDamage}");

        if (__result == null) return;
        if (critPower == null) return;

        // 改 _text 字段（_Ready() 会读它来设 Label 文本，必须赶在 _Ready() 之前改）
        string prefix = EntiyArchiveLibrary.UI.GetText("CRIT_TEXT");
        Traverse.Create(__result).Field("_text").SetValue($"{prefix}{result.UnblockedDamage}");

        // 设颜色（_Ready() 不改颜色，所以直接操作 Label 即可）
        var label = __result.GetNode<MegaLabel>("Label");
        label.AddThemeColorOverride("font_color", new Color("#FFD700"));
        label.AddThemeColorOverride("font_outline_color", new Color("#977d1d"));

        critPower.WasCrit = false;
        LibraryLogger.Info("暴击伤害数字修改成功！");
    }

    /// <summary>在所有 Creature 中查找标记了暴击的 CriticalChanceBenefit 实例。</summary>
    private static CriticalChanceBenefit? FindCritPower(Creature target)
    {
        var combatState = target.CombatState;
        if (combatState == null) return null;

        foreach (var creature in combatState.Creatures)
        {
            var power = creature.GetPower<CriticalChanceBenefit>();
            if (power != null && power.WasCrit)
                return power;
        }
        return null;
    }
}
