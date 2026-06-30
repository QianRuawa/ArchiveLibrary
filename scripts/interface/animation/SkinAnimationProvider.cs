namespace ArchiveLibrary.Scripts.Animation;

/// <summary>皮肤动画集</summary>
public record AnimSet(string Idle, string Attack, string Cast, string Hit, string Dead);

/// <summary>
/// 支持多皮肤的动画提供者。根据当前皮肤名选择对应的 <see cref="AnimSet"/>。
/// </summary>
public class SkinAnimationProvider : ICharacterAnimationProvider
{
    public string AnimationNodePath => _nodePath;
    public string IdleAnimation => GetCurrentSet()?.Idle ?? "";

    private readonly string _nodePath;
    private readonly string _extraNodePath;
    private readonly Dictionary<string, AnimSet> _skins;
    private readonly Func<string> _getCurrentSkin;

    /// <param name="nodePath">主场景中 AnimationPlayer 所在节点的路径</param>
    /// <param name="extraNodePath">额外动画节点的名称（如 "PlayerExtraAnimation"），没有则传 null</param>
    /// <param name="skins">皮肤名 → 动画集</param>
    /// <param name="getCurrentSkin">返回当前皮肤名的委托</param>
    public SkinAnimationProvider(
        string nodePath,
        string? extraNodePath,
        Dictionary<string, AnimSet> skins,
        Func<string> getCurrentSkin)
    {
        _nodePath = nodePath;
        _extraNodePath = extraNodePath ?? "";
        _skins = skins;
        _getCurrentSkin = getCurrentSkin;
    }

    public string GetAnimForTrigger(string trigger)
    {
        var set = GetCurrentSet();
        if (set == null) return "";
        return trigger switch
        {
            "Attack" => set.Attack,
            "Cast" => set.Cast,
            "Hit" => set.Hit,
            "Dead" => set.Dead,
            _ => set.Idle
        };
    }

    /// <summary>额外动画节点名（用于播放特效动画），没有则返回 null</summary>
    public string? ExtraNodeName => string.IsNullOrEmpty(_extraNodePath) ? null : _extraNodePath;

    private AnimSet? GetCurrentSet()
    {
        var skin = _getCurrentSkin();
        return _skins.TryGetValue(skin, out var set) ? set : _skins.Values.FirstOrDefault();
    }
}
