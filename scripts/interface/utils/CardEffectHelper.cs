using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.RichTextTags;
using ArchiveLibrary.Scripts.Localization;

namespace ArchiveLibrary.Scripts.Utils;

/// <summary>
/// 卡牌浮动文字的显示配置。
/// 默认使用 RichTextLabel 支持 BBCode 富文本，颜色等样式在文字中通过标签自定义。
/// </summary>
public record FloatingTextConfig
{
    /// <summary>字体颜色，默认白（颜色通过 BBCode 在文字中自定义）。</summary>
    public Color FontColor { get; set; } = Colors.White;
    /// <summary>字号，默认 28。</summary>
    public int FontSize { get; set; } = 28;
    /// <summary>相对卡牌的偏移，默认 (0, -150)。</summary>
    public Vector2 Position { get; set; } = new(0, -170);
    /// <summary>缩放，默认 1。</summary>
    public Vector2 Scale { get; set; } = Vector2.One * 1f;
    /// <summary>默认使用 RichTextLabel 支持 BBCode 富文本。</summary>
    public bool UseRichText { get; set; } = true;
    /// <summary>富文本自适应宽度，默认 true。</summary>
    public bool RichTextFitContent { get; set; } = true;
    /// <summary>自动换行模式，默认 Off（不换行）。</summary>
    public TextServer.AutowrapMode AutowrapMode { get; set; } = TextServer.AutowrapMode.Off;
    /// <summary>文字轮廓大小，默认 6（0 为无轮廓）。</summary>
    public int OutlineSize { get; set; } = 6;
    /// <summary>文字轮廓颜色，默认黑色。</summary>
    public Color OutlineColor { get; set; } = Colors.Black;
}

/// <summary>
/// 卡牌浮动文字与自定义数据辅助工具。
/// 支持在卡牌上显示临时文字、多语言翻译、富文本，以及为每张卡牌绑定自定义数据字典。
/// </summary>
public static class CardEffectHelper
{
    // ===== 浮动文字管理 =====

    private static readonly Dictionary<int, List<Node>> CardLabels = new();

    /// <summary>为 RichTextLabel 安装原版 STS2 自定义 BBCode 效果（颜色、动画等）。</summary>
    private static void InstallSts2Effects(RichTextLabel label)
    {
        // 颜色标签
        label.InstallEffect(new RichTextGold());
        label.InstallEffect(new RichTextBlue());
        label.InstallEffect(new RichTextRed());
        label.InstallEffect(new RichTextGreen());
        label.InstallEffect(new RichTextAqua());
        label.InstallEffect(new RichTextPurple());
        label.InstallEffect(new RichTextPink());
        label.InstallEffect(new RichTextOrange());
        // 动画标签
        label.InstallEffect(new RichTextSine());
        label.InstallEffect(new RichTextJitter());
        label.InstallEffect(new RichTextFadeIn());
        label.InstallEffect(new RichTextFlyIn());
        label.InstallEffect(new RichTextThinkyDots());
        label.InstallEffect(new RichTextAncientBanner());
    }

    /// <summary>使用默认配置附加浮动文字，支持 BBCode。</summary>
    public static Node? AttachFloatingText(CardModel card, string text)
    {
        return AttachFloatingText(card, text, new FloatingTextConfig());
    }

    /// <summary>使用自定义配置附加浮动文字。</summary>
    public static Node? AttachFloatingText(CardModel card, string text, FloatingTextConfig config)
    {
        var nCard = NCard.FindOnTable(card);
        if (nCard == null) return null;

        Node? label;
        if (config.UseRichText)
        {
            var rich = new RichTextLabel
            {
                Text = text,
                Modulate = config.FontColor,
                Scale = config.Scale,
                FitContent = config.RichTextFitContent,
                AutowrapMode = config.AutowrapMode,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Position = config.Position
            };
            if (config.OutlineSize > 0)
            {
                rich.AddThemeConstantOverride("outline_size", config.OutlineSize);
                rich.AddThemeColorOverride("font_outline_color", config.OutlineColor);
            }
            rich.SetUseBbcode(true);
            InstallSts2Effects(rich);
            label = rich;
        }
        else
        {
            var lbl = new Label
            {
                Text = text,
                Modulate = config.FontColor,
                Scale = config.Scale
            };
            lbl.AddThemeColorOverride("font_color", config.FontColor);
            lbl.AddThemeFontSizeOverride("font_size", config.FontSize);
            lbl.Position = config.Position;
            label = lbl;
        }

        nCard.AddChild(label);

        var id = card.GetHashCode();
        if (!CardLabels.TryGetValue(id, out var list))
        {
            list = new List<Node>();
            CardLabels[id] = list;
        }
        list.Add(label);

        return label;
    }

    /// <summary>使用翻译键名附加浮动文字（默认配置）。</summary>
    public static Node? AttachLocalizedText(CardModel card, string key, params object[] args)
    {
        return AttachLocalizedText(card, key, new FloatingTextConfig(), args);
    }

    /// <summary>使用翻译键名附加浮动文字（自定义配置，自动识别调用方 Mod）。</summary>
    public static Node? AttachLocalizedText(CardModel card, string key, FloatingTextConfig config, params object[] args)
    {
        string text = args.Length > 0 ? UIHelper.Get(key, args) : UIHelper.Get(key);
        return AttachFloatingText(card, text, config);
    }

    /// <summary>移除卡牌上的浮动文字节点。</summary>
    public static void RemoveFloatingText(Node? label)
    {
        if (label != null && label.IsInsideTree())
            label.QueueFree();
    }

    /// <summary>移除指定卡牌上的所有浮动文字。</summary>
    public static void RemoveAllFloatingText(CardModel card)
    {
        var id = card.GetHashCode();
        if (!CardLabels.Remove(id, out var list)) return;

        foreach (var node in list)
        {
            if (node.IsInsideTree())
                node.QueueFree();
        }
    }

    // ===== 卡牌自定义数据字典 =====

    private static readonly Dictionary<int, Dictionary<string, object>> CardDataStore = new();

    /// <summary>为卡牌存储自定义数据。</summary>
    public static void SetData(CardModel card, string key, object? value)
    {
        var id = card.GetHashCode();
        if (!CardDataStore.TryGetValue(id, out var data))
        {
            data = new Dictionary<string, object>();
            CardDataStore[id] = data;
        }

        if (value == null)
            data.Remove(key);
        else
            data[key] = value;
    }

    /// <summary>读取卡牌自定义数据。</summary>
    public static T? GetData<T>(CardModel card, string key)
    {
        var id = card.GetHashCode();
        if (!CardDataStore.TryGetValue(id, out var data)) return default;
        if (!data.TryGetValue(key, out var val)) return default;
        return val is T t ? t : default;
    }

    /// <summary>移除卡牌的所有自定义数据。</summary>
    public static void ClearData(CardModel card)
    {
        CardDataStore.Remove(card.GetHashCode());
    }
}
