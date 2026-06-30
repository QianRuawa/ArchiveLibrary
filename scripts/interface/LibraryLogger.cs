using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace ArchiveLibrary.Scripts.Utils;

/// <summary>
/// 多 Mod 共享日志工具。自动识别调用方 Mod 并显示对应前缀。
/// 各 Mod 在 Init 中调用 <see cref="SetPrefix"/> 设置中文名后，
/// 日志会显示为 >>>[中文名 Mod]。
/// 未设置时自动使用 Mod 的 manifest id。
/// </summary>
public static class LibraryLogger
{
    private static readonly Dictionary<string, string> _prefixes = new();
    private static readonly string _defaultAsmName;

    static LibraryLogger()
    {
        _defaultAsmName = typeof(LibraryLogger).Assembly.GetName().Name ?? "";
        _prefixes[_defaultAsmName] = ">>>[档案库—Mod]";
    }

    /// <summary>设置当前 Mod 的中文日志前缀（在 Init 中调用）。</summary>
    public static void SetPrefix(string chineseName)
    {
        var asmName = Assembly.GetCallingAssembly().GetName().Name ?? "";
        _prefixes[asmName] = $">>>[{chineseName} Mod]";
    }

    private static string GetPrefix()
    {
        var trace = new System.Diagnostics.StackTrace();
        var selfAsm = typeof(LibraryLogger).Assembly;
        for (int i = 0; i < trace.FrameCount; i++)
        {
            var asm = trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
            if (asm == null || asm == selfAsm) continue;
            var name = asm.GetName().Name ?? "";
            if (_prefixes.TryGetValue(name, out var prefix)) return prefix;

            // 未注册 → 从 ModManager 获取 mod id
            var mod = ModManager.Mods.FirstOrDefault(m =>
                m.state == ModLoadState.Loaded && m.assembly == asm);
            var id = mod?.manifest?.id ?? name;
            _prefixes[name] = $">>>[{id} Mod]";
            return _prefixes[name];
        }
        return ">>>[Unknown Mod]";
    }

    public static void Debug(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log.Debug($"{GetPrefix()}{{调试}}: {message} 路径：{GetShortPath(filePath)}:{lineNumber}");
    }

    public static void Info(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log.Info($"{GetPrefix()}{{信息}}: {message} 路径：{GetShortPath(filePath)}:{lineNumber}");
    }

    public static void Warn(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log.Warn($"{GetPrefix()}{{警告}}: {message} 路径：{GetShortPath(filePath)}:{lineNumber}");
    }

    public static void Error(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log.Error($"{GetPrefix()}{{报错}}: {message} 路径：{GetShortPath(filePath)}:{lineNumber}");
    }

    public static void Error(Exception ex, string message = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string msg = string.IsNullOrEmpty(message) ? ex.Message : $"{message} | {ex.Message}";
        Log.Error($"{GetPrefix()}{{报错}}: {msg} 路径：{GetShortPath(filePath)}:{lineNumber}\n{ex.StackTrace}");
    }

    internal static void Error(string message, Exception ex)
    {
        Log.Error($"{GetPrefix()}{{报错}}: {message} 路径：未知\n{ex.StackTrace}");
    }

    private static string GetShortPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return "未知文件";
        int idx = fullPath.IndexOf("Scripts");
        if (idx >= 0) return fullPath.Substring(idx);
        return System.IO.Path.GetFileName(fullPath);
    }
}
