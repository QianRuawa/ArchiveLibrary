namespace ArchiveLibrary.Scripts.Animation;

/// <summary>
/// 角色动画提供者接口。Mod 制作者实现此接口并注册到 <see cref="CharacterAnimationRegistry"/>。
/// </summary>
public interface ICharacterAnimationProvider
{
    /// <summary>待机动画名</summary>
    string IdleAnimation { get; }
    /// <summary>场景中 AnimationPlayer 所在节点的路径（相对于 NCreature）</summary>
    string AnimationNodePath { get; }
    /// <summary>获取对应触发器名称的动画名</summary>
    string GetAnimForTrigger(string trigger);
}
