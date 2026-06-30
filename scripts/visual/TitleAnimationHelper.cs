using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// Boss 标题动画播放辅助工具，支持默认场景和自定义场景。
/// </summary>
public static class TitleAnimationHelper
{
    private const string DefaultTitleScenePath = "res://scene/title/title_BlackScreen.tscn";

    /// <summary>使用默认场景播放标题动画。</summary>
    public static void Play()
    {
        Play(DefaultTitleScenePath);
    }

    /// <summary>使用自定义路径播放标题动画。</summary>
    /// <param name="scenePath">标题动画场景路径</param>
    public static void Play(string scenePath)
    {
        var titleScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (titleScene == null)
        {
            LibraryLogger.Error($"无法加载标题动画场景: >{scenePath}");
            return;
        }

        var titleNode = titleScene.Instantiate();
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
        {
            LibraryLogger.Error("TitleAnimationHelper: NCombatRoom.Instance 为 null");
            return;
        }
        combatRoom.AddChild(titleNode);
    }
}
