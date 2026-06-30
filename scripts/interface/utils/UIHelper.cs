using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Localization;

/// <summary>
/// 多语言翻译辅助工具。使用 JSON 文件加载翻译（key-value 格式），
/// 支持模板参数替换（如 {name}）。
///
/// 使用方式：
/// - Mod Init 中调用 UIHelper.Register(Assembly.GetExecutingAssembly(), "res://MyMod/localization/")
/// - 各处代码直接调用 UIHelper.Get("KEY") 即可自动按调用方 Mod 返回对应翻译
/// - 也可用实例 new UIHelper("res://...") 手动管理
/// </summary>
public class UIHelper
{
    private readonly string _basePath;
    private Dictionary<string, string>? _translations;
    private string? _currentLang;

    private static readonly Dictionary<Assembly, UIHelper> _registry = new();

    /// <summary>可选的全局默认实例，未找到注册 Mod 时的回退。</summary>
    public static UIHelper? Default { get; set; }

    /// <summary>创建翻译实例。</summary>
    /// <param name="basePath">基础路径，如 "res://MyMod/localization/"</param>
    public UIHelper(string basePath)
    {
        _basePath = basePath;
    }

    // ===== 注册系统 =====

    /// <summary>为指定程序集注册翻译实例（推荐在 Mod Init 中调用）。</summary>
    public static UIHelper Register(Assembly assembly, string basePath)
    {
        var instance = new UIHelper(basePath);
        _registry[assembly] = instance;
        return instance;
    }

    /// <summary>注册已有的翻译实例到指定程序集。</summary>
    public static void Register(Assembly assembly, UIHelper instance)
    {
        _registry[assembly] = instance;
    }

    /// <summary>获取指定程序集的翻译实例。</summary>
    public static UIHelper? ForAssembly(Assembly assembly)
    {
        _registry.TryGetValue(assembly, out var instance);
        return instance;
    }

    // ===== 静态兼容 API（自动识别调用方 Mod）=====

    /// <summary>设置基础路径的同时创建默认实例并注册调用方（兼容旧代码）。</summary>
    public static string BasePath
    {
        set
        {
            var instance = new UIHelper(value);
            Default = instance;
            // 自动注册调用方程序集
            var selfAsm = typeof(UIHelper).Assembly;
            var trace = new System.Diagnostics.StackTrace();
            for (int i = 0; i < trace.FrameCount; i++)
            {
                var asm = trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
                if (asm != null && asm != selfAsm)
                {
                    _registry[asm] = instance;
                    break;
                }
            }
        }
    }

    /// <summary>获取翻译文本，自动识别调用方 Mod。</summary>
    public static string Get(string key)
    {
        var instance = DetectCaller();
        return instance?.GetText(key) ?? key;
    }

    /// <summary>获取翻译文本并替换模板参数，自动识别调用方 Mod。</summary>
    public static string Get(string key, params object[] args)
    {
        var instance = DetectCaller();
        return instance?.GetText(key, args) ?? key;
    }

    /// <summary>通过调用栈识别当前是哪个 Mod 在调用。</summary>
    private static UIHelper? DetectCaller()
    {
        var selfAsm = typeof(UIHelper).Assembly;
        var trace = new System.Diagnostics.StackTrace();
        for (int i = 0; i < trace.FrameCount; i++)
        {
            var asm = trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
            if (asm == null || asm == selfAsm) continue;
            if (_registry.TryGetValue(asm, out var instance))
                return instance;
        }
        return Default;
    }

    private static readonly string[] _langCodes = ["zhs", "zht", "eng", "jpn", "kor", "fre", "ger", "spa", "rus", "por", "ita"];

    /// <summary>检查并重新加载翻译（语言变化时自动重载）。</summary>
    private void EnsureLoaded()
    {
        string langCode = GetGameLanguageCode();
        if (_currentLang == langCode && _translations != null)
            return;

        _currentLang = langCode;
        string path = $"{_basePath}{langCode}/ui_translation.json";

        if (!ResourceLoader.Exists(path))
        {
            LibraryLogger.Warn($"UIHelper: 未找到 {path}，回退到英文 (eng)");
            path = $"{_basePath}eng/ui_translation.json";
            if (!ResourceLoader.Exists(path))
            {
                LibraryLogger.Error("UIHelper: 未找到任何 ui_translation.json！使用键名作为默认值。");
                _translations = [];
                return;
            }
        }

        try
        {
            var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            string content = file.GetAsText();
            file.Close();
            _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(content) ?? [];
            LibraryLogger.Info($"UIHelper: 已加载 {_translations.Count} 条翻译从 {path}");
        }
        catch (Exception e)
        {
            LibraryLogger.Error($"UIHelper: 加载 {path} 失败", e);
            _translations = [];
        }
    }

    /// <summary>获取翻译文本。</summary>
    public string GetText(string key)
    {
        EnsureLoaded();
        return _translations.TryGetValue(key, out string? value) ? value : key;
    }

    /// <summary>获取翻译文本并替换模板参数。</summary>
    public string GetText(string key, params object[] args)
    {
        string text = GetText(key);
        if (args.Length % 2 != 0)
        {
            LibraryLogger.Warn($"UIHelper.GetText({key}): 参数个数必须是偶数（name, value 对）");
            return text;
        }
        for (int i = 0; i < args.Length; i += 2)
        {
            string placeholder = "{" + args[i] + "}";
            string value = args[i + 1]?.ToString() ?? "";
            text = text.Replace(placeholder, value);
        }
        return text;
    }

    /// <summary>获取游戏当前语言代码（zhs/eng/jpn/kor/zht）。</summary>
    private static string GetGameLanguageCode()
    {
        try
        {
            var locManager = LocManager.Instance;
            if (locManager != null)
            {
                var prop = locManager.GetType().GetProperty("Language")
                        ?? locManager.GetType().GetProperty("CurrentLocale")
                        ?? locManager.GetType().GetProperty("CurrentLanguage");
                if (prop != null)
                {
                    string? code = prop.GetValue(locManager)?.ToString()?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(code))
                        return code;
                }
            }
        }
        catch { }

        var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
        string name = culture.Name.ToLowerInvariant();

        if (name.StartsWith("zh"))
            return name is "zh-tw" or "zh-hk" ? "zht" : "zhs";
        if (name.StartsWith("ja"))
            return "jpn";
        if (name.StartsWith("ko"))
            return "kor";

        return "eng";
    }
}
