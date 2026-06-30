using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 生物位移能力接口，声明可击退/牵引。
/// </summary>
public interface IMovementEffect
{
    /// <summary>击退目标（向左移动）。</summary>
    Task Knockback(Creature target, float distance = 120f);

    /// <summary>牵引目标（向右移动）。</summary>
    Task MoveRight(Creature target, float distance = 120f);
}

/// <summary>
/// 生物击退/牵引工具，基于 Godot Tween 实现平滑水平位移。
/// 提供 Knockback（左移）、MoveRight（右移）、MoveToX（指定坐标）功能。
/// </summary>
public static class MovementHelper
{
    private static NCreature GetNode(Creature creature)
    {
        return NCombatRoom.Instance?.GetCreatureNode(creature);
    }

    /// <summary>将目标向左击退，持续 0.25 秒，缓出效果。</summary>
    /// <param name="target">目标生物</param>
    /// <param name="distance">位移距离（像素，默认 120）</param>
    public static async Task Knockback(Creature target, float distance = 120f)
    {
        var node = GetNode(target);
        if (node == null) return;
        float newX = node.GlobalPosition.X - distance;
        var tween = node.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(node, "global_position:x", newX, 0.25f);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>将目标向右牵引，持续 0.25 秒，缓出效果。</summary>
    /// <param name="target">目标生物</param>
    /// <param name="distance">位移距离（像素，默认 120）</param>
    public static async Task MoveRight(Creature target, float distance = 120f)
    {
        var node = GetNode(target);
        if (node == null) return;
        float newX = node.GlobalPosition.X + distance;
        var tween = node.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(node, "global_position:x", newX, 0.25f);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>将目标平滑移动到指定 X 坐标，持续 0.35 秒，缓出效果。</summary>
    /// <param name="target">目标生物</param>
    /// <param name="targetX">目标 X 坐标</param>
    public static async Task MoveToX(Creature target, float targetX)
    {
        var node = GetNode(target);
        if (node == null) return;
        var tween = node.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(node, "global_position:x", targetX, 0.35f);
        await tween.ToSignal(tween, Tween.SignalName.Finished);
    }
}
