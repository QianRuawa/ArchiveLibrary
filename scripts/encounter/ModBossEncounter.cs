using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using ArchiveLibrary.Scripts.Utils;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace ArchiveLibrary.Scripts.Encounters;

/// <summary>
/// 自定义 Boss 遭遇基类，自动根据 <see cref="BossId"/> 构建资源路径。
/// 各路径属性均为 virtual，可单独 override 自定义。
/// </summary>
public abstract class ModBossEncounter : EncounterModel, IModEncounterAssetOverrides
{
    /// <summary>Boss ID，用于自动构建默认资源路径。</summary>
    protected abstract string BossId { get; }

    // ===== 可自定义的路径（默认自动根据 BossId 构建） =====

    /// <summary>遭遇场景路径。</summary>
    protected virtual string EncounterScenePath =>
        $"res://scenes/encounters/{BossId}_boss_encounter.tscn";

    /// <summary>Boss 背景场景路径。</summary>
    protected virtual string BossBgScenePath =>
        $"res://scenes/backgrounds/{BossId}_boss_encounter/{BossId}_boss_encounter_background.tscn";

    /// <summary>Boss 背景图层目录。</summary>
    protected virtual string BossBgLayersDir =>
        $"res://scenes/backgrounds/{BossId}_boss_encounter/layers";

    /// <summary>Boss 地图节点路径。</summary>
    protected virtual string BossMapNodePath =>
        $"res://images/map/placeholder/{BossId}_boss";

    /// <summary>运行历史图标路径（默认）。</summary>
    protected virtual string DefaultIconPath
    {
        get
        {
            string stem = $"{BossId}_boss_encounter".ToLowerInvariant();
            return $"res://images/ui/run_history/{stem}.png";
        }
    }

    /// <summary>运行历史图标轮廓路径，自动根据 <see cref="DefaultIconPath"/> 推导。</summary>
    protected virtual string DefaultIconOutlinePath
    {
        get
        {
            string icon = DefaultIconPath;
            return icon.EndsWith(".png")
                ? icon[..^4] + "_outline.png"
                : icon + "_outline";
        }
    }

    // ===== IModEncounterAssetOverrides =====

    public EncounterAssetProfile? AssetProfile => new(
        EncounterScenePath,
        BossBgScenePath,
        BossBgLayersDir,
        BossMapNodePath,
        null,
        null,
        DefaultIconPath,
        DefaultIconOutlinePath
    );

    public string? CustomEncounterScenePath => EncounterScenePath;
    public string? CustomBackgroundScenePath => BossBgScenePath;
    public string? CustomBackgroundLayersDirectoryPath => BossBgLayersDir;
    public string? CustomBossNodePath => BossMapNodePath;

    public IReadOnlyList<string>? CustomExtraAssetPaths => BuildExtraAssetPaths();
    public IReadOnlyList<string>? CustomMapNodeAssetPaths => null;

    public string? CustomRunHistoryIconPath => DefaultIconPath;
    public string? CustomRunHistoryIconOutlinePath => DefaultIconOutlinePath;

    // ===== 原版属性 =====

    public override string BossNodePath => BossMapNodePath;

    public override MegaSkeletonDataResource? BossNodeSpineResource => null;

    protected override bool HasCustomBackground
    {
        get
        {
            bool layersDirExists = DirAccess.Open(BossBgLayersDir) != null;
            LibraryLogger.Info($"[ModBossEncounter] 图层目录({BossBgLayersDir}) 存在: {layersDirExists}");
            return layersDirExists;
        }
    }

    // ===== 预加载资源 =====

    private IReadOnlyList<string>? BuildExtraAssetPaths()
    {
        var paths = new List<string>();

        if (ResourceLoader.Exists(DefaultIconOutlinePath))
            paths.Add(DefaultIconOutlinePath);
        if (ResourceLoader.Exists(DefaultIconPath))
            paths.Add(DefaultIconPath);

        if (ResourceLoader.Exists(BossBgScenePath))
        {
            paths.Add(BossBgScenePath);
            LibraryLogger.Info($"[ModBossEncounter] 预加载背景场景: {BossBgScenePath}");
        }

        if (HasCustomBackground)
        {
            using var dir = DirAccess.Open(BossBgLayersDir);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (fileName.EndsWith(".tscn"))
                    {
                        string layerPath = $"{BossBgLayersDir}/{fileName}";
                        paths.Add(layerPath);
                        LibraryLogger.Info($"[ModBossEncounter] 预加载背景图层: {layerPath}");
                    }
                }
            }
        }

        if (paths.Count > 0)
            LibraryLogger.Info($"[ModBossEncounter] 总预加载资源数: {paths.Count} (BossId={BossId})");
        else
            LibraryLogger.Info($"[ModBossEncounter] 无额外资源需预加载 (BossId={BossId})");

        return paths.Count > 0 ? paths : null;
    }

    public override IEnumerable<string>? ExtraAssetPaths => BuildExtraAssetPaths();
}
