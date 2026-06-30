using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ArchiveLibrary.Scripts.Visual;

/// <summary>
/// 生物视觉节点辅助工具，控制可见性、血条、意图图标等。
/// </summary>
public static class CreatureVisualHelper
{
    /// <summary>设置生物整个视觉节点的可见性（包括模型、血条、意图等）。</summary>
    public static void SetCreatureNodeVisible(Creature creature, bool visible)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature != null)
            nCreature.Visible = visible;
    }

    /// <summary>设置生物的血条可见性。</summary>
    public static void SetHealthBarVisible(Creature creature, bool visible)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return;

        var healthBar = nCreature.GetNodeOrNull<Control>("HealthBar")
                     ?? FindNodeRecursive<Control>(nCreature, "HealthBar");
        if (healthBar != null)
            healthBar.Visible = visible;
    }

    /// <summary>设置生物的意图图标可见性。</summary>
    public static void SetIntentVisible(Creature creature, bool visible)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return;

        var intentsContainer = nCreature.GetNodeOrNull<Control>("Intents")
                            ?? FindNodeRecursive<Control>(nCreature, "Intents");
        if (intentsContainer != null)
            intentsContainer.Visible = visible;
    }

    /// <summary>强制从场景树中移除并销毁生物的视觉节点（NCreature）。</summary>
    public static void RemoveCreatureNode(Creature creature)
    {
        if (creature == null) return;
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null) return;

        var nCreature = combatRoom.GetCreatureNode(creature);
        if (nCreature != null && nCreature.IsInsideTree())
        {
            combatRoom.RemoveCreatureNode(nCreature);
            nCreature.QueueFree();
        }
    }

    /// <summary>递归查找指定名称和类型的子节点。</summary>
    private static T? FindNodeRecursive<T>(Node node, string name) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child.Name == name && child is T t)
                return t;
            var result = FindNodeRecursive<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
