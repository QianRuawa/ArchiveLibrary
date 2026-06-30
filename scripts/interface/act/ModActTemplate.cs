using ArchiveLibrary.Scripts.Map;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace ArchiveLibrary.Scripts.Act;

/// <summary>
/// Mod 新层级基类。继承此类创建新层级（如第4章、第5章等），
/// 层级会自动注册到游戏 Act 列表和图鉴中，无需额外 Harmony 补丁。
///
/// 使用方式:
/// <code>
/// public class ExtraAct : ModActTemplate
/// {
///     public override int Index => 5; // 第5章
///     public override string[] BgMusicOptions => [ "event:/music/my_act_bgm" ];
///     public override IEnumerable<EventModel> AllEvents => ...;
///     public override IEnumerable<EncounterModel> GenerateAllEncounters() => ...;
/// }
/// </code>
/// </summary>
public abstract class ModActTemplate : ActModel
{
    /// <summary>层级索引（如 4=第四章, 5=第五章）。不可与已有层级重复。</summary>
    public abstract override int Index { get; }

    // ===== 解锁 =====
    public override bool IsDefault => false;
    public override bool IsUnlocked(UnlockState unlockState) => true;

    // ===== 地图颜色 =====
    public override Color MapTraveledColor => new Color("00002E");
    public override Color MapUntraveledColor => new Color("1E1E3A");
    public override Color MapBgColor => new Color("3C425E");

    // ===== 背景资源 =====
    /// <summary>背景标识（用于战斗/休息/宝箱背景），默认按 Index 回退：1→Underdocks, 2→Hive, 3→Glory, 其他→Glory。</summary>
    public virtual string BackgroundTheme => Index switch
    {
        1 => "underdocks",
        2 => "hive",
        _ => "glory",
    };

    /// <summary>自定义休息处背景，null 则使用 <see cref="BackgroundTheme"/> 对应原版背景。</summary>
    public virtual Control? CustomRestSiteBackground => null;

    /// <summary>自定义战斗背景资源，null 则使用 <see cref="BackgroundTheme"/> 对应原版背景。</summary>
    public virtual BackgroundAssets? CustomBackgroundAssets => null;

    // ===== 音乐与音效 =====
    /// <summary>FMOD 背景音乐事件（不熟悉 FMOD 的请在 <see cref="CustomMusicPath"/> 设置 MP3/OGG 文件）。</summary>
    public override string[] BgMusicOptions => ModelDb.Act<Glory>().BgMusicOptions;
    /// <summary>FMOD 音乐银行路径。</summary>
    public override string[] MusicBankPaths => ModelDb.Act<Glory>().MusicBankPaths;
    /// <summary>FMOD 环境音效事件。</summary>
    public override string AmbientSfx => "event:/sfx/ambience/act4_ambience";
    public override string ChestOpenSfx => "event:/sfx/ui/chest_open";
    public override string ChestSpineSkinNameNormal => "Normal";
    public override string ChestSpineSkinNameStroke => "Stroke";
    public override string ChestSpineResourcePath => "res://spine/chest/chest_normal/chest_normal_spine_skeletondata.tres";
    /// <summary>自定义层级音乐配置。返回 null 使用 FMOD。</summary>
    public virtual ActMusicProfile? CustomMusic => null;

    // ===== 地图配置 =====
    /// <summary>每层的基础房间数量。</summary>
    protected override int BaseNumberOfRooms => 8;

    /// <summary>自定义房间类型分布。不重写则使用第三层默认值。</summary>
    public virtual MapPointTypeCounts GetCustomMapPointTypes(Rng mapRng)
    {
        var glory = ModelDb.Act<Glory>();
        var counts = glory.GetMapPointTypes(mapRng);
        // 可在此修改 counts 的数值
        return counts;
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) => GetCustomMapPointTypes(mapRng);

    // ===== 自定义地图 =====
    /// <summary>自定义地图是否启用。返回 false 则使用该层级的默认地图。可在此添加配置检查。</summary>
    public virtual bool EnableCustomMap => true;
    /// <summary>自定义地图粒子场景路径（供大退读档时使用，请与地图的 <c>ParticleScenePath</c> 保持一致）。</summary>
    public virtual string? CustomMapParticlePath => null;
    /// <summary>自定义地图 Boss VFX 场景路径。</summary>
    public virtual string? CustomMapBossVfxPath => null;
    /// <summary>自定义地图 Boss VFX 坐标列表。</summary>
    public virtual IEnumerable<(int col, int row)> CustomMapBossVfxPoints => Array.Empty<(int, int)>();
    /// <summary>粒子贴图基础路径（大退读档恢复用）。</summary>
    public virtual string CustomMapTextureBasePath => "res://images/map/particle/";
    /// <summary>粒子场景中需要设置贴图的节点名称。</summary>
    public virtual string CustomMapTextureNodeName => "Kether";
    /// <summary>每个坐标的贴图ID、偏移、缩放（大退读档恢复用）。</summary>
    public virtual IEnumerable<(int col, int row, string id, Vector2 offset, Vector2 scale)> CustomMapVisuals
        => Array.Empty<(int, int, string, Vector2, Vector2)>();
    /// <summary>拟态Boss配置（大退读档恢复用）。</summary>
    public virtual IEnumerable<Map.ModMapTemplate.MimicBossDef> CustomMapMimicBosses
        => Array.Empty<Map.ModMapTemplate.MimicBossDef>();
    /// <summary>自定义地图顶部背景图路径（如 "res://images/packed/map/map_bgs/my_act/map_top_my_act.png"），null 则使用原版。</summary>
    public virtual string? CustomMapTopBgPath => null;
    /// <summary>自定义地图中部背景图路径。</summary>
    public virtual string? CustomMapMidBgPath => null;
    /// <summary>自定义地图底部背景图路径。</summary>
    public virtual string? CustomMapBotBgPath => null;

    /// <summary>辅助方法：创建视觉配置元组，默认 offset=Zero, scale=(0.5,0.5)。</summary>
    protected static (int col, int row, string id, Vector2 offset, Vector2 scale) V(
        int col, int row, string id,
        Vector2 offset = default, Vector2 scale = default)
        => (col, row, id, offset, scale == default ? new Vector2(0.5f, 0.5f) : scale);

    /// <summary>
    /// 创建自定义地图实例。重写此方法返回自定义 <see cref="ActMap"/>（如 <see cref="Map.ModMapTemplate"/> 子类），
    /// 返回 null 则使用游戏默认地图。该实例会被 <c>Hook.ModifyGeneratedMap</c> 自动注入，无需手动写 Harmony 补丁。
    /// <code>
    /// public override ActMap? CreateCustomMap(RunState runState) => new MyMap(runState);
    /// </code>
    /// </summary>
    public virtual ActMap? CreateCustomMap(RunState runState) => null;

    // ===== 先古 =====
    /// <summary>本层专属先古事件。</summary>
    public abstract override IEnumerable<AncientEventModel> AllAncients { get; }

    /// <summary>固定事件映射（坐标 → 事件类型）。在地图节点上触发固定事件。</summary>
    internal static readonly Dictionary<(int Col, int Row), Type> FixedEvents = new();

    /// <summary>绑定固定事件：玩家走到 (col,row) 时强制触发指定事件。</summary>
    protected void SetFixedEvent<T>(int col, int row) where T : EventModel
    {
        FixedEvents[(col, row)] = typeof(T);
    }

    /// <summary>是否拦截游戏自带的共享先古（如达弗），默认 true 拦截。</summary>
    public virtual bool BlockSharedAncients => true;

    /// <summary>是否允许本层在 A10+ 获得第二个 Boss，默认 false（由原版第3层保留双Boss）。</summary>
    public virtual bool AllowDoubleBoss => false;

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
    {
        return AllAncients.ToList();
    }

    // ===== 事件 =====
    public abstract override IEnumerable<EventModel> AllEvents { get; }

    // ===== 遭遇战 =====
    // <summary>注意：如果是直接使用原版层级的遭遇战，而且是只想显示自己自定义的，请一定清理原版添加在里面的Boss类，不然会生成自定义Boss。</summary>
    public abstract override IEnumerable<EncounterModel> GenerateAllEncounters();

    // ===== Boss（请重写，返回本层 Boss 遭遇战列表）=====
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => Array.Empty<EncounterModel>();

    // ===== 应用解锁发现顺序 =====
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }

    // ===== 未知节点类型 =====
    /// <summary>
    /// 过滤未知（?）房间的可选类型。默认强制为 Event，确保固定事件坐标不被随机化覆盖。
    /// 若需允许 ? 房间出现其他类型（如怪物、商店），请重写此方法。
    /// </summary>
    public override IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
    {
        return new HashSet<RoomType> { RoomType.Event };
    }
}

/// <summary>层级音乐配置（像 CharacterAssetProfile 一样，一个对象包含所有参数）。</summary>
/// <param name="MusicPath">音频文件路径（MP3/OGG）</param>
/// <param name="VolumeDb">音量 dB（0=最大，-6=一半）</param>
/// <param name="FadeIn">淡入秒数</param>
/// <param name="FadeOut">淡出秒数</param>
/// <param name="LoopStart">循环起点（秒，0=从头）</param>
/// <param name="LoopEnd">循环终点（秒，0=播到尾循环）</param>
public record class ActMusicProfile(
    string MusicPath,
    float VolumeDb = -6f,
    float FadeIn = 1.5f,
    float FadeOut = 1.5f,
    double LoopStart = 0f,
    double LoopEnd = 0f
);
