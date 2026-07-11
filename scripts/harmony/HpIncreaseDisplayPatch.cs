using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// Harmony 补丁：单位持有 HpIncrease 时，在血条右侧显示层数图标和数值，血条边框变绿。
/// 不干扰左侧原版格挡显示。所有视觉参数在下方 Settings 字典中集中配置。
/// </summary>
[HarmonyPatch]
static class HpIncreaseDisplayPatch
{
    static readonly Godot.Collections.Dictionary Settings = new()
    {
        ["IconPath"] = "res://images/Combat_Icon_Error.png",
        ["IconScaleX"] = 0.5,
        ["IconScaleY"] = 0.5,
        ["ContainerOffsetX"] = -12.0,
        ["ContainerOffsetY"] = 12.0,
        ["IconWidth"] = 24.0,
        ["IconHeight"] = 24.0,
        ["IconOffsetX"] = -24.0,
        ["IconOffsetY"] = -28.0,
        ["BorderColor"] = "#c5d750",
    };

    private const string NodeName = "HpIncreaseDisplay";

    private static readonly Texture2D _iconTexture;
    private static readonly Color _borderColor;
    private static readonly float _containerOffsetX;
    private static readonly float _containerOffsetY;
    private static readonly float _iconWidth;
    private static readonly float _iconHeight;
    private static readonly float _iconOffsetX;
    private static readonly float _iconOffsetY;
    private static readonly Vector2 _iconScale;
    /// <summary>保存血条原始的格挡框颜色，退出 HpIncrease 状态时恢复。</summary>
    private static readonly Dictionary<ulong, Color> _originalBlockColors = new();

    static HpIncreaseDisplayPatch()
    {
        var iconPath = Settings.TryGetValue("IconPath", out var ip) ? ip.AsString() : "res://images/Combat_Icon_Error.png";
        _iconTexture = ResourceLoader.Load<Texture2D>(iconPath);

        var hex = Settings.TryGetValue("BorderColor", out var bc) ? bc.AsString() : "#5BBD5B";
        _borderColor = new Color(hex);

        _containerOffsetX = Settings.TryGetValue("ContainerOffsetX", out var cox) ? (float)cox.AsDouble() : 10f;
        _containerOffsetY = Settings.TryGetValue("ContainerOffsetY", out var coy) ? (float)coy.AsDouble() : 0f;
        _iconWidth = Settings.TryGetValue("IconWidth", out var iw) ? (float)iw.AsDouble() : 60f;
        _iconHeight = Settings.TryGetValue("IconHeight", out var ih) ? (float)ih.AsDouble() : 60f;
        _iconOffsetX = Settings.TryGetValue("IconOffsetX", out var iox) ? (float)iox.AsDouble() : 0f;
        _iconOffsetY = Settings.TryGetValue("IconOffsetY", out var ioy) ? (float)ioy.AsDouble() : 0f;

        var sx = Settings.TryGetValue("IconScaleX", out var isx) ? (float)isx.AsDouble() : 1f;
        var sy = Settings.TryGetValue("IconScaleY", out var isy) ? (float)isy.AsDouble() : 1f;
        _iconScale = new Vector2(sx, sy);
    }

    [HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
    [HarmonyPostfix]
    static void OnRefreshBlockUi(NHealthBar __instance)
    {
        var creature = AccessTools.Field(typeof(NHealthBar), "_creature").GetValue(__instance) as Creature;
        if (creature == null) return;
        var hpIncrease = creature.GetPower<HpModifier>();
        bool hasHpIncrease = hpIncrease != null && hpIncrease.Amount > 0;

        var existing = __instance.GetNodeOrNull<Control>(NodeName);
        if (hasHpIncrease && existing == null)
            RefreshDisplay(__instance, updatePos: true); // 首次创建时定位
        else if (hasHpIncrease && existing != null)
            RefreshDisplay(__instance, updatePos: false); // 已有节点只更新文字
        else
            RefreshDisplay(__instance, updatePos: false); // 清除
    }

    [HarmonyPatch(typeof(NHealthBar), "UpdateLayoutForCreatureBounds")]
    [HarmonyPostfix]
    static void OnUpdateLayout(NHealthBar __instance)
    {
        RefreshDisplay(__instance, updatePos: true); // 布局变化时重新定位
    }

    /// <summary>创建/刷新额外血量显示。updatePos=false 时仅更新层数文字，避免位置抖动。</summary>
    static void RefreshDisplay(NHealthBar __instance, bool updatePos)
    {
        var creature = AccessTools.Field(typeof(NHealthBar), "_creature").GetValue(__instance) as Creature;
        if (creature == null) return;

        var hpIncrease = creature.GetPower<HpModifier>();
        bool hasHpIncrease = hpIncrease != null && hpIncrease.Amount > 0;

        var blockOutline = AccessTools.Field(typeof(NHealthBar), "_blockOutline").GetValue(__instance) as Control;
        var existing = __instance.GetNodeOrNull<Control>(NodeName);

        if (hasHpIncrease)
        {
            // 强制显示格挡框并变色（即使格挡为0），只存一次原始颜色
            if (blockOutline != null)
            {
                if (!_originalBlockColors.ContainsKey(__instance.GetInstanceId()))
                    _originalBlockColors[__instance.GetInstanceId()] = blockOutline.Modulate;
                blockOutline.Visible = true;
                blockOutline.Modulate = _borderColor;
            }

            Control container = existing;
            if (container == null)
            {
                container = CreateDisplayNode(__instance);
                __instance.AddChild(container);
                updatePos = true;
            }

            if (updatePos) UpdatePosition(__instance, container);
            container.GetNode<Label>("AmountLabel").Text = hpIncrease.Amount.ToString();
            container.Visible = true;
        }
        else
        {
            // 恢复原始的格挡框颜色，移除显示节点
            if (blockOutline != null)
            {
                if (_originalBlockColors.TryGetValue(__instance.GetInstanceId(), out var originalColor))
                    blockOutline.Modulate = originalColor;
                _originalBlockColors.Remove(__instance.GetInstanceId());
            }
            if (existing != null)
                existing.QueueFree();
        }
    }

    /// <summary>创建图标 + 层数标签的容器节点。</summary>
    static Control CreateDisplayNode(NHealthBar __instance)
    {
        var containerSize = new Vector2(_iconWidth, _iconHeight);

        var container = new Control
        {
            Name = NodeName,
            Size = containerSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var icon = new TextureRect
        {
            Name = "Icon",
            Texture = _iconTexture,
            Position = new Vector2(_iconOffsetX, _iconOffsetY),
            Size = new Vector2(_iconWidth, _iconHeight),
            Scale = _iconScale,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        container.AddChild(icon);

        var label = new Label
        {
            Name = "AmountLabel",
            Size = containerSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", new Color("#dbe8db"));
        label.AddThemeColorOverride("font_outline_color", new Color("#6ebf45"));
        label.AddThemeConstantOverride("outline_size", 14);
        label.AddThemeFontSizeOverride("font_size", 24);
        container.AddChild(label);

        return container;
    }

    /// <summary>将显示容器定位到血条右侧。</summary>
    static void UpdatePosition(NHealthBar __instance, Control container)
    {
        var hpBarContainer = __instance.HpBarContainer;
        var blockContainer = AccessTools.Field(typeof(NHealthBar), "_blockContainer").GetValue(__instance) as Control;
        if (blockContainer == null) return;

        float rightX = hpBarContainer.GlobalPosition.X + hpBarContainer.Size.X + _containerOffsetX;
        float posY = blockContainer.GlobalPosition.Y + _containerOffsetY;
        container.GlobalPosition = new Vector2(rightX, posY);
    }
}
