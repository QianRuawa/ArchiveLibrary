using System;
using ArchiveLibrary.Scripts.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ArchiveLibrary.Scripts.Hooks;

/// <summary>
/// 卡牌升级事件。在任何卡牌被升级时触发（战斗升级、篝火升级、全局升级）。
/// 通过 <see cref="CardUpgradedEvent.OnUpgraded"/> 事件或 <see cref="CardUpgradedHelper"/> 监听。
/// </summary>
public static class CardUpgradedEvent
{
    /// <summary>卡牌升级时触发。参数：(升级的卡牌, 升级预览样式)</summary>
    public static event Action<CardModel, CardPreviewStyle>? OnUpgraded;

    internal static void Invoke(CardModel card, CardPreviewStyle style)
    {
        OnUpgraded?.Invoke(card, style);
    }

    internal static int ListenerCount => OnUpgraded?.GetInvocationList()?.Length ?? 0;
}

/// <summary>补丁：拦截 CardCmd.Upgrade 并触发升级事件。</summary>
[HarmonyPatch(typeof(CardCmd))]
public static class CardUpgradedHook
{
    [HarmonyPatch(nameof(CardCmd.Upgrade), typeof(CardModel), typeof(CardPreviewStyle))]
    [HarmonyPostfix]
    public static void Postfix(CardModel card, CardPreviewStyle style)
    {
        LibraryLogger.Info($"CardUpgradedHook: 卡牌 [{card.Id?.Entry}] 升级触发, 样式={style}, 监听者数量={CardUpgradedEvent.ListenerCount}");
        CardUpgradedEvent.Invoke(card, style);
    }
}

/// <summary>
/// 升级事件辅助工具。简化订阅升级事件的写法。
/// </summary>
public static class CardUpgradedHelper
{
    /// <summary>注册一个卡牌升级回调（所有升级来源：战斗、篝火、全局）。</summary>
    public static void OnAnyUpgrade(Action<CardModel> handler)
    {
        CardUpgradedEvent.OnUpgraded += (card, _) => handler(card);
    }

    /// <summary>注册一个"战斗升级"回调（从战斗奖励中选择升级，GridLayout）。</summary>
    public static void OnCombatUpgrade(Action<CardModel> handler)
    {
        CardUpgradedEvent.OnUpgraded += (card, style) =>
        {
            if (style == CardPreviewStyle.GridLayout)
                handler(card);
        };
    }

    /// <summary>注册一个"非战斗升级"回调（篝火锻造、事件升级等）。</summary>
    public static void OnNonCombatUpgrade(Action<CardModel> handler)
    {
        CardUpgradedEvent.OnUpgraded += (card, style) =>
        {
            if (style != CardPreviewStyle.GridLayout)
                handler(card);
        };
    }
}
