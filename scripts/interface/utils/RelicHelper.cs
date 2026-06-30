using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.Utils;

/// <summary>
/// 遗物相关的辅助工具。
/// </summary>
public static class RelicHelper
{
    /// <summary>
    /// 如果生物拥有指定类型的遗物，则执行 action。
    /// </summary>
    public static async Task<bool> IfHasRelic<T>(Creature? creature, Func<Task> action) where T : RelicModel
    {
        if (creature?.Player?.Relics.Any(r => r is T) == true)
        {
            await action();
            return true;
        }
        return false;
    }
}
