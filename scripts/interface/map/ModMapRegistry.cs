using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using System.Reflection;

namespace ArchiveLibrary.Scripts.Map;

internal static class MimicBossHelper
{
    private static readonly PropertyInfo _pointProp = typeof(NNormalMapPoint).GetProperty("Point",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo IconField = typeof(NNormalMapPoint).GetField("_icon",
        BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo OutlineField = typeof(NNormalMapPoint).GetField("_outline",
        BindingFlags.NonPublic | BindingFlags.Instance);
    internal static readonly FieldInfo ContainerField = typeof(NNormalMapPoint).GetField("_iconContainer",
        BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool IsMimic(NNormalMapPoint point)
    {
        if (_pointProp?.GetValue(point) is not MapPoint mp) return false;
        return ModMapTemplate.MimicBosses.Any(m => m.Col == mp.coord.col && m.Row == mp.coord.row);
    }

    public static void ApplyVisual(NNormalMapPoint instance)
    {
        if (_pointProp?.GetValue(instance) is not MapPoint mp) return;

        var def = ModMapTemplate.MimicBosses.FirstOrDefault(m => m.Col == mp.coord.col && m.Row == mp.coord.row);
        if (def.Col == 0 && def.Row == 0 && !ModMapTemplate.MimicBosses.Any(m => m.Col == mp.coord.col && m.Row == mp.coord.row))
            return;

        if (ResourceLoader.Exists(def.IconPath) && IconField?.GetValue(instance) is TextureRect icon)
        {
            icon.Texture = ResourceLoader.Load<Texture2D>(def.IconPath);
            icon.Scale = Vector2.One * def.Scale;
        }
        if (ResourceLoader.Exists(def.OutlinePath) && OutlineField?.GetValue(instance) is TextureRect outline)
            outline.Texture = ResourceLoader.Load<Texture2D>(def.OutlinePath);
        if (def.Tint.HasValue && IconField?.GetValue(instance) is TextureRect tinted)
            tinted.SelfModulate = def.Tint.Value;
        if (ContainerField?.GetValue(instance) is Control container)
            container.Scale = Vector2.One * def.Scale;
    }
}

[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready))]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_ReadyPatch
{
    public static void Postfix(NNormalMapPoint __instance) => MimicBossHelper.ApplyVisual(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_UpdateIconPatch
{
    public static void Postfix(NNormalMapPoint __instance) => MimicBossHelper.ApplyVisual(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), "AnimHover")]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_HoverPatch
{
    public static bool Prefix(NNormalMapPoint __instance) => !MimicBossHelper.IsMimic(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), "AnimUnhover")]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_UnhoverPatch
{
    public static bool Prefix(NNormalMapPoint __instance) => !MimicBossHelper.IsMimic(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), "AnimPressDown")]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_PressDownPatch
{
    public static bool Prefix(NNormalMapPoint __instance) => !MimicBossHelper.IsMimic(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), "OnSelected")]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_OnSelectedPatch
{
    public static bool Prefix(NNormalMapPoint __instance) => !MimicBossHelper.IsMimic(__instance);
}

[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Process))]
[HarmonyPriority(Priority.Low)]
public static class MimicBoss_ProcessPatch
{
    private static Texture2D? _cachedIcon;
    private static Texture2D? _cachedOutline;
    private static string _lastIconPath = "";
    private static string _lastOutlinePath = "";

    public static void Postfix(NNormalMapPoint __instance)
    {
        if (!MimicBossHelper.IsMimic(__instance)) return;
        var def = ModMapTemplate.MimicBosses.FirstOrDefault(m =>
            m.Col == __instance.Point.coord.col && m.Row == __instance.Point.coord.row);
        if (def.Col == 0 && def.Row == 0) return;

        // 缓存贴图
        if (_lastIconPath != def.IconPath && ResourceLoader.Exists(def.IconPath))
        { _cachedIcon = ResourceLoader.Load<Texture2D>(def.IconPath); _lastIconPath = def.IconPath; }
        if (_lastOutlinePath != def.OutlinePath && ResourceLoader.Exists(def.OutlinePath))
        { _cachedOutline = ResourceLoader.Load<Texture2D>(def.OutlinePath); _lastOutlinePath = def.OutlinePath; }

        // 每帧重设图标、颜色、缩放（防止被其他方法覆盖）
        if (MimicBossHelper.IconField?.GetValue(__instance) is TextureRect icon)
        {
            if (_cachedIcon != null) icon.Texture = _cachedIcon;
            if (def.Tint.HasValue) icon.SelfModulate = def.Tint.Value;
        }
        if (_cachedOutline != null && MimicBossHelper.OutlineField?.GetValue(__instance) is TextureRect outline)
            outline.Texture = _cachedOutline;
        if (MimicBossHelper.ContainerField?.GetValue(__instance) is Control c)
            c.Scale = Vector2.One * def.Scale;
    }
}
