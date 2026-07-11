using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ArchiveLibrary.Scripts.Map;
using ArchiveLibrary.Scripts.Utils;
using MegaCrit.Sts2.Core.Hooks;
using System.Reflection;

namespace ArchiveLibrary.Scripts.Act;

/// <summary>
/// 新层级（Act）注册中心。Mod 制作者继承 <see cref="ActModel"/> 创建新层级后，
/// 会自动注册到游戏 Act 列表和图鉴中，无需手动编写 Harmony 补丁。
/// </summary>
public static class ModActRegistry
{
    private static List<Type>? _actTypes;

    public static void Register<T>() where T : ActModel
    {
        _actTypes ??= new List<Type>();
        if (!_actTypes.Contains(typeof(T)))
            _actTypes.Add(typeof(T));
    }

    internal static List<Type> GetActs()
    {
        if (_actTypes != null) return _actTypes;
        _actTypes = new List<Type>();

        foreach (var mod in MegaCrit.Sts2.Core.Modding.ModManager.Mods)
        {
            if (mod.state != MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded || mod.assembly == null)
                continue;
            foreach (var type in mod.assembly.GetTypes())
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(ActModel))) continue;
                if (type.Namespace?.StartsWith("MegaCrit") == true) continue;

                try
                {
                    var method = typeof(ModelDb).GetMethod("Act", Type.EmptyTypes)
                        ?.MakeGenericMethod(type);
                    if (method?.Invoke(null, null) is not ActModel act) continue;

                    bool alreadyExists = ModelDb.Acts.Any(a => a.Index == act.Index);
                    if (!alreadyExists)
                    {
                        _actTypes.Add(type);
                        LibraryLogger.Info($"Mod 层级: 已注册 {type.Name} (Index={act.Index})");
                    }
                }
                catch { }
            }
        }
        return _actTypes;
    }

    internal static ActModel? GetAct(Type type)
    {
        try
        {
            var method = typeof(ModelDb).GetMethod("Act", Type.EmptyTypes)
                ?.MakeGenericMethod(type);
            return method?.Invoke(null, null) as ActModel;
        }
        catch { return null; }
    }
}

[HarmonyPatch(typeof(ActModel), "GetDefaultList")]
public static class ActList_GetDefaultList_Patch
{
    static void Postfix(ref IReadOnlyList<ActModel> __result)
    {
        var list = __result.ToList();
        foreach (var type in ModActRegistry.GetActs())
        {
            if (list.Any(a => a.GetType() == type)) continue;
            var act = ModActRegistry.GetAct(type);
            if (act != null) list.Add(act);
            LibraryLogger.Info($"Act列表: 已添加 {type.Name}");
        }
        __result = list;
    }
}

[HarmonyPatch(typeof(ActModel), "GetRandomList")]
public static class ActList_GetRandomList_Patch
{
    static void Postfix(ref IEnumerable<ActModel> __result)
    {
        var list = __result.ToList();
        foreach (var type in ModActRegistry.GetActs())
        {
            if (list.Any(a => a.GetType() == type)) continue;
            var act = ModActRegistry.GetAct(type);
            if (act != null) list.Add(act);
        }
        __result = list;
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class ActAncientCleanupPatch
{
    private static readonly FieldInfo _sharedSubsetField = typeof(ActModel)
        .GetField("_sharedAncientSubset", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Prefix(ActModel __instance)
    {
        if (__instance is ModActTemplate act && act.BlockSharedAncients)
        {
            _sharedSubsetField?.SetValue(__instance, new List<AncientEventModel>());
            LibraryLogger.Debug($"先古清理: {act.GetType().Name} 已拦截共享先古");
        }
    }
}

[HarmonyPatch(typeof(NBestiary), "CreateEntries")]
public static class ActBestiaryPatch
{
    static void Postfix(NBestiary __instance)
    {
        var addAct = typeof(NBestiary)
            .GetMethod("AddAct", BindingFlags.NonPublic | BindingFlags.Instance);
        if (addAct == null) return;

        foreach (var type in ModActRegistry.GetActs())
        {
            var act = ModActRegistry.GetAct(type);
            if (act == null) continue;
            if (!SaveManager.Instance.Progress.DiscoveredActs.Contains(act.Id))
            {
                if (SaveManager.Instance.Progress.DiscoveredActs is HashSet<ModelId> hashSet)
                    hashSet.Add(act.Id);
            }
            addAct.Invoke(__instance, [act]);
            LibraryLogger.Info($"图鉴: 已添加 {type.Name}");
        }
    }
}

[HarmonyPatch(typeof(RunManager), "GenerateRooms")]
public static class ActDoubleBossFixPatch
{
    static void Postfix(RunManager __instance)
    {
        try
        {
            var stateProp = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance);
            var runState = stateProp?.GetValue(__instance) as RunState;
            if (runState == null) return;

            var modActs = runState.Acts?.OfType<ModActTemplate>().ToList() ?? new();
            bool anyModActAllowsDouble = modActs.Any(a => a.AllowDoubleBoss);
            var glory = runState.Acts?.OfType<Glory>().FirstOrDefault();

            if (runState.AscensionLevel >= 10 && glory != null)
            {
                if (!anyModActAllowsDouble && !glory.HasSecondBoss)
                {
                    var rng = runState.Rng?.UpFront;
                    if (rng != null)
                    {
                        var second = rng.NextItem(glory.AllBossEncounters
                            .Where(e => e.Id != glory.BossEncounter.Id)
                            .Where(e => CustomBossRegistry.IsBossEnabledForEncounter(e)));
                        if (second != null)
                        {
                            glory.SetSecondBossEncounter(second);
                            LibraryLogger.Info($"双Boss修复: 已为 Glory 添加第二Boss (A10+)");
                        }
                    }
                }
                foreach (var modAct in modActs)
                {
                    if (!modAct.AllowDoubleBoss && modAct.HasSecondBoss)
                    {
                        var field = typeof(ActModel).GetField("<HasSecondBoss>k__BackingField",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(modAct, false);
                            LibraryLogger.Info($"双Boss修复: 已移除 {modAct.GetType().Name} 的第二Boss");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LibraryLogger.Error($"双Boss修复异常: {ex.Message}");
        }
    }
}

/// <summary>地图粒子再生：读档恢复时延迟再生粒子特效（覆盖 QuickRestart + 主菜单继续）。</summary>
[HarmonyPatch(typeof(RunManager), "SetUpSavedSingleplayer", typeof(RunState), typeof(SerializableRun))]
public static class ActRunRestorePatch
{
    static void Postfix()
    {
        // 从 ModActTemplate 获取粒子路径（覆盖大退后缓存为空的情况）
        if (string.IsNullOrEmpty(ModMapTemplate.LastParticleScenePath))
        {
            var stateProp = typeof(RunManager).GetProperty("State", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateProp?.GetValue(RunManager.Instance) is RunState state)
            {
                foreach (var act in state.Acts.OfType<ModActTemplate>())
                {
                    if (act.CustomMapParticlePath != null)
                    {
                        ModMapTemplate.LastParticleScenePath = act.CustomMapParticlePath;
                        ModMapTemplate.LastBossVfxScenePath = act.CustomMapBossVfxPath;
                        ModMapTemplate.LastBossVfxPoints.Clear();
                        ModMapTemplate.LastBossVfxPoints.AddRange(act.CustomMapBossVfxPoints);
                        ModMapTemplate.LastTextureBasePath = act.CustomMapTextureBasePath;
                        ModMapTemplate.LastTextureNodeName = act.CustomMapTextureNodeName;
                        ModMapTemplate.LastVisuals.Clear();
                        ModMapTemplate.LastVisuals.AddRange(act.CustomMapVisuals);
                        ModMapTemplate.MimicBosses.Clear();
                        ModMapTemplate.MimicBosses.AddRange(act.CustomMapMimicBosses);
                    }
                }
            }
        }

        // 重试直到场景节点就绪
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null) return;
        int retries = 0;
        Action check = null;
        check = () =>
        {
            if (RunManager.Instance == null) return;
            ModMapTemplate.RegenerateFromCache(tree.Root);
            LibraryLogger.Info("粒子已再生[SL读档]");
            if (++retries < 6)
                tree.CreateTimer(0.5f).Timeout += check;
        };
        tree.CreateTimer(0.5f).Timeout += check;
    }
}

/// <summary>自定义层级背景修复：自动按 <see cref="ModActTemplate.BackgroundTheme"/> 回退对应原版背景。</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.CreateRestSiteBackground))]
public static class ActRestSiteBgPatch
{
    public static bool Prefix(ActModel __instance, ref Control __result)
    {
        if (__instance is ModActTemplate act)
        {
            var custom = act.CustomRestSiteBackground;
            if (custom != null) { __result = custom; return false; }

            __result = GetFallbackAct(act.BackgroundTheme).CreateRestSiteBackground();
            return false;
        }
        return true;
    }

    private static ActModel GetFallbackAct(string theme) => theme switch
    {
        "underdocks" => ModelDb.Act<Underdocks>(),
        "hive" => ModelDb.Act<Hive>(),
        _ => ModelDb.Act<Glory>(),
    };
}

/// <summary>自定义层级战斗背景修复。</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
public static class ActBackgroundAssetsPatch
{
    public static bool Prefix(ActModel __instance, Rng rng, ref BackgroundAssets __result)
    {
        if (__instance is ModActTemplate act)
        {
            var custom = act.CustomBackgroundAssets;
            if (custom != null) { __result = custom; return false; }

            __result = new BackgroundAssets(act.BackgroundTheme, rng);
            return false;
        }
        return true;
    }
}

/// <summary>自定义地图三层背景：替换 ModActTemplate 的 MapTopBg/MapMidBg/MapBotBg。</summary>
[HarmonyPatch(typeof(ActModel), "get_MapTopBg")]
public static class ActMapTopBgPatch
{
    static void Postfix(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is ModActTemplate act && act.CustomMapTopBgPath != null && ResourceLoader.Exists(act.CustomMapTopBgPath))
            __result = ResourceLoader.Load<Texture2D>(act.CustomMapTopBgPath);
    }
}

[HarmonyPatch(typeof(ActModel), "get_MapMidBg")]
public static class ActMapMidBgPatch
{
    static void Postfix(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is ModActTemplate act && act.CustomMapMidBgPath != null && ResourceLoader.Exists(act.CustomMapMidBgPath))
            __result = ResourceLoader.Load<Texture2D>(act.CustomMapMidBgPath);
    }
}

[HarmonyPatch(typeof(ActModel), "get_MapBotBg")]
public static class ActMapBotBgPatch
{
    static void Postfix(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is ModActTemplate act && act.CustomMapBotBgPath != null && ResourceLoader.Exists(act.CustomMapBotBgPath))
            __result = ResourceLoader.Load<Texture2D>(act.CustomMapBotBgPath);
    }
}

/// <summary>
/// 自定义地图自动注入：<see cref="ModActTemplate"/> 子类重写 <see cref="ModActTemplate.CreateCustomMap"/>
/// 后由本补丁自动替换默认地图，无需手动编写 <c>Hook.ModifyGeneratedMap</c> 补丁。
/// 通过 <see cref="ModActTemplate.EnableCustomMap"/> 可控制是否启用自定义地图。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGeneratedMap))]
public static class ActCustomMapPatch
{
    public static void Postfix(IRunState runState, int actIndex, ref ActMap __result)
    {
        if (runState.Acts[actIndex] is not ModActTemplate act) return;
        if (!act.EnableCustomMap) return;

        var customMap = act.CreateCustomMap(runState as RunState);
        if (customMap == null) return;

        __result = customMap;
        LibraryLogger.Info($"自定义地图注入: {act.GetType().Name} → {customMap.GetType().Name}");
    }
}

/// <summary>固定事件：Mod 层级地图节点绑定固定事件。</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
public static class ActFixedEventPatch
{
    public static bool Prefix(ActModel __instance, RunState runState, ref EventModel __result)
    {
        if (__instance is not ModActTemplate) return true;
        var point = runState.CurrentMapPoint;
        if (point == null) return true;

        var key = (point.coord.col, point.coord.row);
        if (!ModActTemplate.FixedEvents.TryGetValue(key, out var eventType)) return true;

        var method = typeof(ModelDb).GetMethod("Event", Type.EmptyTypes)?.MakeGenericMethod(eventType);
        if (method?.Invoke(null, null) is EventModel evt)
        {
            __result = evt;
            return false;
        }
        return true;
    }
}

