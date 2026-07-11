using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace ArchiveLibrary.Scripts.Intents;

/// <summary>
/// 自定义意图悬浮提示框接口。
/// 实现后可修改简介框的标题、描述和图标，不影响怪物头上的意图图标。
/// </summary>
public interface ICustomIntentDescription
{
    /// <summary>自定义本地化 key 前缀，同时控制悬浮框的 .title 和 .description。</summary>
    string? CustomLocPrefix { get; set; }
}

/// <summary>
/// 意图描述 fallback 辅助。自动检测 intents.json 中是否存在 .description，
/// 若存在则使用自定义描述，否则 fallback 到通用攻击描述。
/// </summary>
public static class IntentDescriptionHelper
{
    private static readonly HashSet<string> _registeredDescriptions = new();
    private static bool _initialized = false;

    /// <summary>
    /// 扫描本地化基础目录下所有语言子目录中的 intents.json，
    /// 自动注册所有定义了 .description 的前缀。
    /// 例如传入 "res://KasumizawaMiyu/localization/" 会自动扫描
    /// zhs/intents.json、eng/intents.json、ja/intents.json ……
    /// </summary>
    public static void ScanAllLocales(string localizationBasePath)
    {
        var dir = DirAccess.Open(localizationBasePath);
        if (dir == null) return;

        string basePath = localizationBasePath.TrimEnd('/');
        string[] locales = dir.GetDirectories();
        foreach (var locale in locales)
            AutoRegisterFromJson(basePath + "/" + locale + "/intents.json");
    }

    /// <summary>
    /// 扫描单个 intents.json 文件，注册其中所有定义了 .description 的前缀。
    /// </summary>
    public static void AutoRegisterFromJson(string jsonPath)
    {
        if (!Godot.FileAccess.FileExists(jsonPath)) return;

        using var file = Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null) return;

        string content = file.GetAsText();
        if (string.IsNullOrEmpty(content)) return;

        var json = Json.ParseString(content);
        if (json.VariantType != Variant.Type.Dictionary) return;

        var dict = (Godot.Collections.Dictionary)json;
        foreach (var key in dict.Keys)
        {
            string keyStr = key.AsString();
            if (keyStr.EndsWith(".description"))
            {
                string prefix = keyStr.Replace(".description", "");
                if (!string.IsNullOrEmpty(prefix))
                    _registeredDescriptions.Add(prefix);
            }
        }
    }

    /// <summary>注册一个 CustomLocPrefix，表明其 .description 已定义。</summary>
    public static void RegisterDescription(string prefix)
    {
        if (!string.IsNullOrEmpty(prefix))
            _registeredDescriptions.Add(prefix);
    }

    /// <summary>检查指定前缀是否已注册 .description。</summary>
    public static bool HasDescription(string? prefix)
    {
        return prefix != null && _registeredDescriptions.Contains(prefix);
    }

    /// <summary>
    /// 获取意图描述的 LocString。
    /// 若 customPrefix 已注册 .description，则使用自定义描述；
    /// 否则 fallback 到 <paramref name="fallbackKey"/>。
    /// </summary>
    public static LocString GetDescription(string? customPrefix, string fallbackKey)
    {
        string key = (customPrefix != null && _registeredDescriptions.Contains(customPrefix))
            ? customPrefix + ".description"
            : fallbackKey;
        return new LocString("intents", key);
    }
}
