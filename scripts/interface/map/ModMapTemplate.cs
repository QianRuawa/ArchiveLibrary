using ArchiveLibrary.Scripts.Utils;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using System.Reflection;

namespace ArchiveLibrary.Scripts.Map;

/// <summary>
/// 自定义地图基类。继承此类可快速创建自定义层级地图（网格布局、节点特效、背景美化）。
///
/// 使用方式:
/// <code>
/// public class MyMap : ModMapTemplate
/// {
///     public MyMap(RunState state) : base(state) { }
///
///     // 布局：7列 x 9行，(列,行) → 节点类型
///     protected override void BuildMap()
///     {
///         _start = new MapPoint(3, 0) { PointType = MapPointType.Ancient };
///         _boss  = new MapPoint(3, 8) { PointType = MapPointType.Boss };
///         var a = CreatePoint(3, 1, MapPointType.Monster);
///         _start.AddChildPoint(a);
///         a.AddChildPoint(_boss);
///     }
///
///     // 节点视觉配置（可选）
///     protected override void SetupPointVisuals()
///     {
///         SetPointStyle(3, 1, "kether");
///     }
/// }
/// </code>
/// </summary>
public abstract class ModMapTemplate : ActMap
{
    private MapPoint?[,] _grid;

    public override MapPoint StartingMapPoint => _start;
    public override MapPoint BossMapPoint => _boss;
    public override MapPoint? SecondBossMapPoint => null;
    protected override MapPoint?[,] Grid => _grid;

    protected MapPoint _start;
    protected MapPoint _boss;

    /// <summary>网格列数（默认 7）</summary>
    protected virtual int GridCols => 7;
    /// <summary>网格行数（默认 9）</summary>
    protected virtual int GridRows => 9;

    /// <summary>粒子背景场景路径（null=不生成）</summary>
    protected virtual string? ParticleScenePath => null;
    /// <summary>Boss VFX 场景路径（null=不生成）</summary>
    protected virtual string? BossVfxScenePath => null;
    /// <summary>粒子贴图基础路径。默认 "res://images/map/particle/"。</summary>
    protected virtual string TextureBasePath => "res://images/map/particle/";
    /// <summary>粒子场景中需要设置贴图的节点名称，默认 "Kether"。</summary>
    protected virtual string TextureNodeName => "Kether";

    private readonly List<(int col, int row, string id, Vector2 offset, Vector2 scale)> _visuals = new();
    private readonly List<(int col, int row)> _bossVfxPoints = new();
    internal static readonly List<(int col, int row, string id, Vector2 offset, Vector2 scale)> LastVisuals = new();
    internal static string LastTextureBasePath = "";
    internal static string LastTextureNodeName = "Kether";

    /// <summary>拟态Boss节点列表（视觉上变为Boss图标，不改变房间类型）</summary>
    internal static readonly List<MimicBossDef> MimicBosses = new();

    public readonly record struct MimicBossDef(int Col, int Row, string IconPath, string OutlinePath, Color? Tint, float Scale);

    /// <summary>拟态Boss默认色调（未指定时用此值，null=不染色）</summary>
    protected Color? DefaultMimicTint { get; set; }

    /// <summary>玩家开关：是否输出地图节点日志（默认开启）</summary>
    public static bool EnableNodeLogging { get; set; } = true;
    /// <summary>上次使用的粒子场景路径（供 SL 读档再生使用）</summary>
    internal static string? LastParticleScenePath { get; set; }
    /// <summary>上次使用的 Boss VFX 场景路径</summary>
    internal static string? LastBossVfxScenePath { get; set; }
    /// <summary>Boss VFX 坐标列表（供 SL 再生使用）</summary>
    internal static readonly List<(int col, int row)> LastBossVfxPoints = new();

    protected ModMapTemplate(RunState runState)
    {
        _grid = new MapPoint[GridCols, GridRows];
        BuildMap();
        SetupPointVisuals();
        LogMapNodes();
        LastParticleScenePath = ParticleScenePath;
        LastBossVfxScenePath = BossVfxScenePath;
        LastBossVfxPoints.Clear();
        LastBossVfxPoints.AddRange(_bossVfxPoints);
        LastVisuals.Clear();
        LastVisuals.AddRange(_visuals);
        LastTextureBasePath = TextureBasePath;
        LastTextureNodeName = TextureNodeName;

        // 首次加载生成粒子
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree != null)
        {
            var root = tree.Root;
            tree.CreateTimer(0.1f).Timeout += () =>
            {
                GenerateEffects(root);
                LibraryLogger.Info($"粒子已生成[首次加载]");
            };
        }
    }

    /// <summary>在此方法中创建网格点和连接关系。</summary>
    protected abstract void BuildMap();

    /// <summary>在此方法中调用 <see cref="SetPointStyle"/> 配置节点视觉。</summary>
    protected virtual void SetupPointVisuals() { }

    /// <summary>输出地图所有节点的类型和连接关系。</summary>
    protected void LogMapNodes()
    {
        if (!EnableNodeLogging) return;
        for (int row = 0; row < GridRows; row++)
        {
            for (int col = 0; col < GridCols; col++)
            {
                var point = _grid[col, row];
                if (point == null) continue;
                var children = string.Join(", ", point.Children.Select(c => $"({c.coord.col},{c.coord.row})"));
                LibraryLogger.Info($"({col},{row}) {point.PointType} 子节点:[{children}]");
            }
        }
        LibraryLogger.Info($"=== 结束 ===");
    }

    /// <summary>创建一个网格点。</summary>
    protected MapPoint CreatePoint(int col, int row, MapPointType type)
    {
        var point = new MapPoint(col, row) { PointType = type, CanBeModified = false };
        _grid[col, row] = point;
        return point;
    }

    /// <param name="offset">粒子偏移（默认 <see cref="Vector2.Zero"/>）</param>
    /// <param name="scale">粒子缩放（默认 <c>(0.5, 0.5)</c>）</param>
    protected void SetPointStyle(int col, int row, string id, Vector2 offset, Vector2 scale)
    {
        _visuals.Add((col, row, id, offset, scale));
    }

    /// <inheritdoc cref="SetPointStyle(int, int, string, Vector2, Vector2)"/>
    protected void SetPointStyle(int col, int row, string id) =>
        SetPointStyle(col, row, id, Vector2.Zero, new Vector2(0.5f, 0.5f));

    /// <summary>标记某坐标为 Boss VFX 位置。</summary>
    protected void AddBossVfx(int col, int row)
    {
        _bossVfxPoints.Add((col, row));
    }

    /// <summary>将某节点设为拟态Boss（视觉变为Boss图标，仍保持原房间类型）。</summary>
    /// <param name="tint">色调（null=使用 <see cref="DefaultMimicTint"/>）</param>
    protected void SetMimicBoss(int col, int row, string iconPath, string outlinePath, Color? tint = null, float scale = 2.8f)
    {
        MimicBosses.Add(new MimicBossDef(col, row, iconPath, outlinePath, tint ?? DefaultMimicTint, scale));
    }

    // ===== 粒子生成（地图加载后自动调用）=====

    /// <summary>使用缓存的粒子路径在根节点上生成特效（供 SL 读档使用，不依赖地图实例）。</summary>
    internal static void RegenerateFromCache(Node rootNode)
    {
        if (string.IsNullOrEmpty(LastParticleScenePath)) return;
        var tree = rootNode.GetTree();
        if (tree.GetNodesInGroup("mod_map_effects").Count > 0) return;
        var bgScene = ResourceLoader.Load<PackedScene>(LastParticleScenePath);
        if (bgScene == null) return;
        PackedScene? bossVfx = !string.IsNullOrEmpty(LastBossVfxScenePath)
            ? ResourceLoader.Load<PackedScene>(LastBossVfxScenePath) : null;

        Action<Node> traverse = null;
        traverse = node =>
        {
            var prop = node.GetType().GetProperty("Point", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop?.GetValue(node) is MapPoint pt && node is Control control && control.GetParent() is Node parent)
            {
                // 跳过先古起始点
                if (pt.PointType == MapPointType.Ancient) return;

                int col = pt.coord.col;
                int row = pt.coord.row;

                // Boss VFX
                if (LastBossVfxPoints.Any(p => p.col == col && p.row == row) && bossVfx != null)
                {
                    var vfx = bossVfx.Instantiate();
                    parent.AddChild(vfx);
                    vfx.AddToGroup("mod_map_effects");
                    if (vfx is Node2D vfx2d)
                        vfx2d.Position = control.Position;
                    return;
                }

                // 普通粒子
                var bg = bgScene.Instantiate();
                parent.AddChild(bg);
                bg.AddToGroup("mod_map_effects");
                if (bg is Node2D bg2d)
                {
                    var vis = LastVisuals.FirstOrDefault(v => v.col == col && v.row == row);
                    bg2d.Position = control.Position + vis.offset;
                    bg2d.Scale = vis.scale == Vector2.Zero ? new Vector2(0.5f, 0.5f) : vis.scale;
                    if (!string.IsNullOrEmpty(vis.id))
                        SetSpriteTextureStatic(bg, vis.id);
                }
            }
            foreach (var c in node.GetChildren())
                traverse(c);
        };
        traverse(rootNode);
    }

    /// <summary>在地图节点上生成粒子/特效。构造后手动调用。</summary>
    public void GenerateEffects(Node rootNode)
    {
        LibraryLogger.Info($"粒子系统: 场景={ParticleScenePath ?? "空"}");
        if (string.IsNullOrEmpty(ParticleScenePath)) { LibraryLogger.Info("粒子系统: 路径为空，跳过"); return; }

        var bgScene = ResourceLoader.Load<PackedScene>(ParticleScenePath);
        if (bgScene == null) { LibraryLogger.Info($"粒子系统: 场景加载失败 {ParticleScenePath}"); return; }

        LibraryLogger.Info("粒子系统: 场景加载成功");
        PackedScene? bossVfx = null;
        if (!string.IsNullOrEmpty(BossVfxScenePath))
        {
            bossVfx = ResourceLoader.Load<PackedScene>(BossVfxScenePath);
            LibraryLogger.Info($"粒子系统: BossVFX {(bossVfx != null ? "加载成功" : "加载失败")}");
        }

        // 清理旧粒子
        var tree = rootNode.GetTree();
        foreach (Node n in tree.GetNodesInGroup("mod_map_effects")) n.QueueFree();

        int count = TraverseAndSpawn(rootNode, bgScene, bossVfx);
        LibraryLogger.Info($"粒子系统: 遍历完成，生成粒子节点数={count}");
    }

    private int TraverseAndSpawn(Node node, PackedScene bgScene, PackedScene? bossVfx)
    {
        int count = 0;
        MapPoint? point = null;
        var prop = node.GetType().GetProperty("Point",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) point = prop.GetValue(node) as MapPoint;

        if (point != null && node is Control control)
        {
            // 跳过起始先古点，不给它生成特效
            if (point == _start) return 0;

            int col = point.coord.col;
            int row = point.coord.row;

            // Boss VFX
            if (_bossVfxPoints.Any(p => p.col == col && p.row == row) && bossVfx != null)
            {
                var parent = control.GetParent();
                if (parent != null)
                {
                    var vfx = bossVfx.Instantiate();
                    parent.AddChild(vfx);
                    vfx.AddToGroup("mod_map_effects");
                    if (vfx is Node2D vfx2d)
                        vfx2d.Position = control.Position;
                }
                return 1;
            }

            // 粒子背景
            var bg = bgScene.Instantiate();
            var bgParent = control.GetParent();
            if (bgParent != null)
            {
                bgParent.AddChild(bg);
                bg.AddToGroup("mod_map_effects");
                if (bg is Node2D bg2d)
                {
                    var visual = _visuals.FirstOrDefault(v => v.col == col && v.row == row);
                    bg2d.Position = control.Position + visual.offset;
                    bg2d.Scale = visual.scale == Vector2.Zero ? new Vector2(0.5f, 0.5f) : visual.scale;

                    if (!string.IsNullOrEmpty(visual.id))
                    {
                        SetSpriteTexture(bg, visual.id);
                    }
                }
            }
            count++;
        }

        foreach (var child in node.GetChildren())
            count += TraverseAndSpawn(child, bgScene, bossVfx);
        return count;
    }

    private void SetSpriteTexture(Node root, string id)
    {
        // 按名称查找节点（不依赖固定路径，兼容不同场景结构）
        foreach (var child in FindChildrenByName(root, TextureNodeName))
        {
            string path = $"{TextureBasePath}{id}.png";
            if (!ResourceLoader.Exists(path)) continue;

            if (child is Sprite2D s)
                s.Texture = ResourceLoader.Load<Texture2D>(path);
            else if (child is TextureRect t)
                t.Texture = ResourceLoader.Load<Texture2D>(path);
        }
    }

    private static void SetSpriteTextureStatic(Node root, string id)
    {
        foreach (var child in FindChildrenByName(root, LastTextureNodeName))
        {
            string path = $"{LastTextureBasePath}{id}.png";
            if (!ResourceLoader.Exists(path)) continue;
            if (child is Sprite2D s)
                s.Texture = ResourceLoader.Load<Texture2D>(path);
            else if (child is TextureRect t)
                t.Texture = ResourceLoader.Load<Texture2D>(path);
        }
    }

    private static IEnumerable<Node> FindChildrenByName(Node parent, string name)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child.Name?.ToString() == name)
                yield return child;
            foreach (var nested in FindChildrenByName(child, name))
                yield return nested;
        }
    }
}
