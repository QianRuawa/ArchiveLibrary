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
