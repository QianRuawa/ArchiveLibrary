using ArchiveLibrary.Scripts.Map;
using ArchiveLibrary.Scripts.Localization;
using ArchiveLibrary.Scripts.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Content.Patches;
using System.Reflection;

namespace ArchiveLibrary.Scripts;

[ModInitializer(nameof(Init))]
public class EntiyArchiveLibrary
{
    public const string ModId = "ArchiveLibrary";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    /// <summary>本 Mod 的翻译实例。</summary>
    public static UIHelper UI { get; } = new UIHelper($"res://{ModId}/localization/");

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, RitsuLibFramework.CreateLogger(ModId));
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 注册翻译实例（自动识别调用方 Mod 用）
        UIHelper.Register(assembly, UI);
        UIHelper.Default = UI;

		var harmony = new Harmony("sts2.reme.testmod");
		harmony.PatchAll();

        RegisterIconProviders();

        LibraryLogger.Info("已加载/添加 档案库[ArchiveLibrary]（RitsuLib）");
        StartParticleWatcher();
    }

    private static void StartParticleWatcher()
    {
        var tree = Godot.Engine.GetMainLoop() as Godot.SceneTree;
        if (tree == null) return;

        // 每秒检测一次地图节点，有 ModMapTemplate 的粒子缓存就生成（覆盖新游戏、SL、重新开始等所有场景）
        Action check = null;
        check = () =>
        {
            ModMapTemplate.RegenerateFromCache(tree.Root);
            tree.CreateTimer(1.0f).Timeout += check;
        };
        tree.CreateTimer(0.5f).Timeout += check;
    }

    /// <summary>
    /// 注册动态图标纹理 provider，所有实现了 <see cref="IDynamicIconPower"/> 的 Power
    /// 会自动根据 Amount 正负切换图标，无需额外注册。
    /// </summary>
    private static void RegisterIconProviders()
    {
        ExternalAssetOverrideRegistry.RegisterPowerIconTextureProvider(
            ModId + "_DynamicIcon",
            power =>
            {
                if (power is not Powers.IDynamicIconPower dyn)
                    return null;

                string path = power.Amount >= 0 ? dyn.PositiveIconPath : dyn.NegativeIconPath;

                return Godot.ResourceLoader.Load<Godot.Texture2D>(path, null, Godot.ResourceLoader.CacheMode.Reuse);
            }
        );

        ExternalAssetOverrideRegistry.RegisterPowerBigIconTextureProvider(
            ModId + "_DynamicBigIcon",
            power =>
            {
                if (power is not Powers.IDynamicIconPower dyn)
                    return null;

                string path = power.Amount >= 0 ? dyn.PositiveIconPath : dyn.NegativeIconPath;

                return Godot.ResourceLoader.Load<Godot.Texture2D>(path, null, Godot.ResourceLoader.CacheMode.Reuse);
            }
        );
    }
}
