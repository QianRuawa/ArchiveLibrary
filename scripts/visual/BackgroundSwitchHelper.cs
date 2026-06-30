using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 可切换战斗背景的接口。实现此接口的类可调用快速切换或动画切换。
/// </summary>
public interface IBackgroundSwitchable
{
    /// <summary>直接切换背景（不播放动画，立即隐藏其他）。</summary>
    void SwitchBackgroundImmediate(string backgroundPartName);

    /// <summary>通过播放动画切换背景（动画内部控制可见性）。</summary>
    Task SwitchBackgroundWithAnimation(string backgroundPartName, string animName);
}

/// <summary>
/// 战斗背景切换辅助工具，支持快速切换和动画切换。
/// </summary>
public static class BackgroundHelper
{
    /// <summary>获取当前战斗的背景节点。</summary>
    public static NCombatBackground? GetCombatBackground() => NCombatRoom.Instance?.Background;

    /// <summary>直接切换：隐藏所有其他子节点，只显示目标节点。</summary>
    public static void SwitchImmediate(NCombatBackground background, string nodeName)
    {
        if (background == null) return;
        foreach (var child in background.GetChildren())
        {
            if (child is CanvasItem canvasItem)
                canvasItem.Visible = false;
        }
        var target = background.GetNodeOrNull<CanvasItem>(nodeName);
        if (target != null)
            target.Visible = true;
        else
            LibraryLogger.Error($"BackgroundHelper: 未找到节点 {nodeName}");
    }

    /// <summary>播放动画切换：仅播放目标节点下的 AnimationPlayer。</summary>
    public static async Task PlaySwitchAnimation(NCombatBackground background, string nodeName, string animName)
    {
        if (background == null) return;
        var target = background.GetNodeOrNull<CanvasItem>(nodeName);
        if (target == null)
        {
            LibraryLogger.Error($"BackgroundHelper: 未找到节点 {nodeName}");
            return;
        }
        var animPlayer = target.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animPlayer != null && animPlayer.HasAnimation(animName))
        {
            animPlayer.Play(animName);
        }
        else
        {
            LibraryLogger.Error($"BackgroundHelper: 节点 {nodeName} 下没有 AnimationPlayer 或动画 '{animName}'");
        }
    }
}
