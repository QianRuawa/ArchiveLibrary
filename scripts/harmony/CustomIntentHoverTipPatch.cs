using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ArchiveLibrary.Scripts.Intents;

/// <summary>
/// 拦截 <see cref="AbstractIntent.GetHoverTip"/>，当 Intent 设置了
/// <see cref="ICustomIntentDescription.CustomLocPrefix"/> 时，
/// 替换悬浮框的标题。
/// 只影响悬浮提示框，不影响怪物头上的意图图标。
/// </summary>
[HarmonyPatch(typeof(AbstractIntent), "GetHoverTip")]
public static class CustomIntentHoverTipPatch
{
    public static void Postfix(AbstractIntent __instance, IEnumerable<Creature> targets, Creature owner, ref HoverTip __result)
    {
        if (__instance is ICustomIntentDescription { CustomLocPrefix: not null } custom)
        {
            var title = new LocString("intents", custom.CustomLocPrefix + ".title");
            __result = new HoverTip(title, __result.Description, __result.Icon);
        }
    }
}
