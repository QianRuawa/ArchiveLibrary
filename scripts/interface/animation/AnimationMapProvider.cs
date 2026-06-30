namespace ArchiveLibrary.Scripts.Animation;

/// <summary>
/// 简单的动画映射提供者。通过字典配置触发器 → 动画名的映射。
/// </summary>
public class AnimationMapProvider : ICharacterAnimationProvider
{
    public string IdleAnimation { get; }
    public string AnimationNodePath { get; }
    private readonly Dictionary<string, string> _animationMap;

    public AnimationMapProvider(string idleAnim, string nodePath, Dictionary<string, string> animMap)
    {
        IdleAnimation = idleAnim;
        AnimationNodePath = nodePath;
        _animationMap = animMap;
    }

    public string GetAnimForTrigger(string trigger)
    {
        return _animationMap.TryGetValue(trigger, out var anim) ? anim : IdleAnimation;
    }
}
