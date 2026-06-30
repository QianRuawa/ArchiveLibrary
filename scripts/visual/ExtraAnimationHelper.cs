using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 额外动画播放辅助工具（PlayerExtraAnimation 节点），
/// 适用于需要独立于主动画的附加动画播放。
/// </summary>
public static class ExtraAnimationHelper
{
    /// <summary>直接播放额外动画（不等待完成）。</summary>
    /// <param name="creature">目标生物</param>
    /// <param name="animName">动画名</param>
    /// <param name="speedScale">播放速度</param>
    public static void Play(Creature creature, string animName, float speedScale = 1.0f)
    {
        var animPlayer = GetAnimationPlayer(creature);
        if (animPlayer == null)
        {
            LibraryLogger.Error($"ExtraAnimationHelper: 未找到 <<{creature.Name}>> 的 PlayerExtraAnimation");
            return;
        }

        if (!animPlayer.HasAnimation(animName))
        {
            var names = new List<string>();
            foreach (var n in animPlayer.GetAnimationList())
                names.Add(n.ToString());
            LibraryLogger.Error($"ExtraAnimationHelper: 动画 <<{animName}>> 不存在。可用: {string.Join(", ", names)}");
            return;
        }

        animPlayer.SpeedScale = speedScale;
        animPlayer.Play(animName);
    }

    /// <summary>播放额外动画并等待完成，然后切换到待机动画。</summary>
    /// <param name="creature">目标生物</param>
    /// <param name="animName">动画名</param>
    /// <param name="speedScale">播放速度</param>
    /// <param name="idleAnimName">完成后切换的待机动画（可选）</param>
    public static async Task PlayAsync(Creature creature, string animName, float speedScale = 1.0f, string idleAnimName = null)
    {
        var animPlayer = GetAnimationPlayer(creature);
        if (animPlayer == null) return;
        if (!animPlayer.HasAnimation(animName)) return;

        float originalSpeed = animPlayer.SpeedScale;
        animPlayer.SpeedScale = speedScale;
        animPlayer.Play(animName);
        await animPlayer.ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);
        animPlayer.SpeedScale = originalSpeed;

        if (!string.IsNullOrEmpty(idleAnimName) && animPlayer.HasAnimation(idleAnimName))
            animPlayer.Play(idleAnimName);
    }

    /// <summary>停止额外动画。</summary>
    public static void Stop(Creature creature)
    {
        var animPlayer = GetAnimationPlayer(creature);
        animPlayer?.Stop();
    }

    // ===== 内部方法 =====

    private static AnimationPlayer GetAnimationPlayer(Creature creature)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature)
                     ?? NBestiary.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return null;

        var extraNode = FindChildByName(nCreature, "PlayerExtraAnimation");
        if (extraNode == null) return null;

        return FindAnimationPlayer(extraNode);
    }

    private static Node FindChildByName(Node parent, string name)
    {
        if (parent.Name == name) return parent;
        foreach (var child in parent.GetChildren())
        {
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player) return player;
        foreach (var child in node.GetChildren())
        {
            var result = FindAnimationPlayer(child);
            if (result != null) return result;
        }
        return null;
    }
}
