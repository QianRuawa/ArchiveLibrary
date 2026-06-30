using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ArchiveLibrary.Scripts.Animation;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 通用 3D 角色动画补丁。拦截 <see cref="NCreature.SetAnimationTrigger"/>，
/// 通过 <see cref="CharacterAnimationRegistry"/> 查找已注册的角色动画提供者并播放。
/// </summary>
[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
public static class Character3DAnimationPatch
{
    public static void Postfix(NCreature __instance, string trigger)
    {
        if (__instance?.Entity == null) return;

        var provider = CharacterAnimationRegistry.GetProvider(__instance.Entity.ModelId.Entry);
        if (provider == null) return;

        PlayMainAnim(__instance, provider, trigger);
        PlayExtraAnim(__instance, provider, trigger);
    }

    private static void PlayMainAnim(NCreature node, ICharacterAnimationProvider provider, string trigger)
    {
        var visual = node.GetNodeOrNull<Node2D>(provider.AnimationNodePath);
        if (visual == null) return;

        var animPlayer = FindAnimationPlayer(visual);
        if (animPlayer == null) return;

        string animName = provider.GetAnimForTrigger(trigger);
        if (string.IsNullOrEmpty(animName) || !animPlayer.HasAnimation(animName)) return;

        animPlayer.Stop();
        animPlayer.SpeedScale = 0.8f;
        animPlayer.Play(animName, 1f);

        if (trigger != "Dead" && !string.IsNullOrEmpty(provider.IdleAnimation))
        {
            string idle = provider.IdleAnimation;
            animPlayer.Connect(
                AnimationMixer.SignalName.AnimationFinished,
                Callable.From((StringName finishedAnim) =>
                {
                    if (finishedAnim == animName && animPlayer.HasAnimation(idle))
                    {
                        animPlayer.SpeedScale = 0.8f;
                        animPlayer.Play(idle);
                    }
                }),
                (uint)GodotObject.ConnectFlags.OneShot
            );
        }
    }

    private static void PlayExtraAnim(NCreature node, ICharacterAnimationProvider provider, string trigger)
    {
        // 额外动画（SkinAnimationProvider 支持）
        string? extraNodeName = (provider as SkinAnimationProvider)?.ExtraNodeName;
        if (extraNodeName == null) return;

        var extraPlayer = FindAnimationPlayerByName(node, extraNodeName);
        if (extraPlayer == null || !extraPlayer.IsInsideTree()) return;
        if (!extraPlayer.HasAnimation(trigger)) return;

        extraPlayer.Stop();
        extraPlayer.Play(trigger);

        if (trigger != "Dead")
        {
            extraPlayer.Connect(
                AnimationMixer.SignalName.AnimationFinished,
                Callable.From((StringName finishedAnim) =>
                {
                    if (finishedAnim != "Idle" && extraPlayer.HasAnimation("Idle"))
                        extraPlayer.Play("Idle");
                }),
                (uint)GodotObject.ConnectFlags.OneShot
            );
        }
    }

    private static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player) return player;
        foreach (var child in node.GetChildren())
        {
            var result = FindAnimationPlayer(child);
            if (result != null) return result;
        }
        return null;
    }

    private static AnimationPlayer? FindAnimationPlayerByName(Node parent, string name)
    {
        if (parent is AnimationPlayer anim && anim.Name == name) return anim;
        foreach (var child in parent.GetChildren())
        {
            var result = FindAnimationPlayerByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
