using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// Boss 动画播放辅助工具，支持播放攻击/死亡动画并等待完成。
/// </summary>
public static class BossAnimationHelper
{
    /// <summary>播放攻击动画，等待完成，然后恢复待机。</summary>
    /// <param name="creature">目标生物</param>
    /// <param name="attackAnimName">攻击动画名</param>
    /// <param name="idleAnimName">待机动画名（默认 idle）</param>
    /// <param name="speedScale">播放速度</param>
    /// <param name="originalspeedScale">恢复后的速度（0 表示保持原速）</param>
    public static async Task PlayAttackAsync(Creature creature, string attackAnimName, string idleAnimName = "idle", float speedScale = 0.6f, float originalspeedScale = 0)
    {
        var animPlayer = GetAnimationPlayer(creature);
        if (animPlayer == null)
        {
            LibraryLogger.Error($"PlayAttackAsync: 未找到 <<{creature.Name}>> 的 AnimationPlayer");
            return;
        }

        if (!animPlayer.HasAnimation(attackAnimName))
        {
            LibraryLogger.Error($"PlayAttackAsync: 动画 <<{attackAnimName}>> 不存在");
            return;
        }

        float originalSpeed = animPlayer.SpeedScale;
        animPlayer.SpeedScale = speedScale;
        animPlayer.Play(attackAnimName);
        await animPlayer.ToSignal(animPlayer, AnimationMixer.SignalName.AnimationFinished);
        animPlayer.SpeedScale = originalSpeed;
        if (originalspeedScale != 0) animPlayer.SpeedScale = originalspeedScale;

        if (creature.IsAlive && animPlayer.HasAnimation(idleAnimName))
            animPlayer.Play(idleAnimName);
    }

    /// <summary>播放死亡动画，可选同步位移。</summary>
    /// <param name="creature">目标生物</param>
    /// <param name="deathAnimName">死亡动画名</param>
    /// <param name="speedScale">播放速度</param>
    /// <param name="moveOffset">位移偏移（可选）</param>
    /// <param name="movementSpeed">位移速度（秒）</param>
    public static async Task PlayDeathAsync(Creature creature, string deathAnimName, float speedScale = 1.0f, Vector2 moveOffset = default, float movementSpeed = 1.0f)
    {
        var animPlayer = GetAnimationPlayer(creature);
        if (animPlayer == null)
        {
            LibraryLogger.Error($"PlayDeathAsync: 未找到 <<{creature.Name}>> 的 AnimationPlayer");
            return;
        }

        animPlayer.Stop();

        if (!animPlayer.HasAnimation(deathAnimName))
        {
            LibraryLogger.Error($"PlayDeathAsync: 动画 <<{deathAnimName}>> 不存在");
            return;
        }

        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature)
                     ?? NBestiary.Instance?.GetCreatureNode(creature);
        Node2D visuals = null;
        if (nCreature != null)
        {
            var wakamoNode = nCreature.GetNodeOrNull<Node>("wakamo");
            if (wakamoNode != null)
                visuals = wakamoNode.GetNodeOrNull<Node2D>("Visuals");
            if (visuals == null)
                visuals = nCreature.GetNodeOrNull<Node2D>("wakamo");
        }

        float originalSpeed = animPlayer.SpeedScale;
        animPlayer.SpeedScale = speedScale;
        animPlayer.Play(deathAnimName);

        if (moveOffset != Vector2.Zero && visuals != null)
        {
            var startPos = visuals.Position;
            var tween = visuals.CreateTween();
            tween.TweenProperty(visuals, "position", startPos + moveOffset, movementSpeed)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Quad);
        }
        else if (moveOffset != Vector2.Zero)
        {
            LibraryLogger.Info("BossAnimationHelper: 未找到 Visuals 节点，无法移动");
        }

        await animPlayer.ToSignal(animPlayer, AnimationMixer.SignalName.AnimationFinished);
        animPlayer.SpeedScale = originalSpeed;
    }

    /// <summary>获取生物的 AnimationPlayer（优先 NCombatRoom，后备 NBestiary）。</summary>
    private static AnimationPlayer GetAnimationPlayer(Creature creature)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature)
                     ?? NBestiary.Instance?.GetCreatureNode(creature);
        if (nCreature == null)
        {
            LibraryLogger.Error($"GetAnimationPlayer: 无法获取 creature {creature?.Name} 的视觉节点");
            return null;
        }

        var result = FindAnimationPlayer(nCreature);
        if (result == null)
            LibraryLogger.Error("GetAnimationPlayer: 递归查找后未找到有效的 AnimationPlayer");
        return result;
    }

    /// <summary>递归查找 AnimationPlayer，跳过不可见的 Node3D 子树。</summary>
    private static AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is Node3D node3D && !node3D.Visible)
            return null;
        if (node is AnimationPlayer animPlayer)
        {
            if (GodotObject.IsInstanceValid(animPlayer))
                return animPlayer;
        }
        foreach (var child in node.GetChildren())
        {
            var result = FindAnimationPlayer(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
