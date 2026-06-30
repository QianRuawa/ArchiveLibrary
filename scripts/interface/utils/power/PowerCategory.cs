using Godot;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// Buff 类型分类。
/// 决定图标路径的文件夹和文件名前缀。
/// </summary>
public enum PowerCategory
{
    /// <summary>正面效果 → images/benefit/Combat_Icon_Buff_{id}.png</summary>
    Benefit,
    /// <summary>负面效果 → images/adverse/Combat_Icon_Debuff_{id}.png</summary>
    Adverse,
    /// <summary>控制效果 → images/crowdControl/Combat_Icon_CC_{id}.png</summary>
    CrowdControl,
    /// <summary>特殊效果 → images/particular/Combat_Icon_Special_{id}.png</summary>
    Particular,
}

public static class PowerCategoryHelper
{
    private static readonly Dictionary<PowerCategory, string> FolderMap = new()
    {
        [PowerCategory.Benefit] = "benefit",
        [PowerCategory.Adverse] = "adverse",
        [PowerCategory.CrowdControl] = "crowdControl",
        [PowerCategory.Particular] = "particular",
    };

    private static readonly Dictionary<PowerCategory, string> PrefixMap = new()
    {
        [PowerCategory.Benefit] = "Buff",
        [PowerCategory.Adverse] = "Debuff",
        [PowerCategory.CrowdControl] = "CC",
        [PowerCategory.Particular] = "Special",
    };

    private const string ErrorIconPath = "res://images/Combat_Icon_Error.png";

    /// <summary>自动生成图标路径：res://images/{folder}/Combat_Icon_{prefix}_{iconId}.png</summary>
    public static string GetIconPath(this PowerCategory category, string iconId)
    {
        return $"res://images/{FolderMap[category]}/Combat_Icon_{PrefixMap[category]}_{iconId}.png";
    }

    /// <summary>
    /// 获取图标路径，文件不存在时返回 Error 占位图路径。
    /// </summary>
    public static string ResolveIconPath(this PowerCategory category, string iconId)
    {
        string path = GetIconPath(category, iconId);
        return ResourceLoader.Exists(path) ? path : ErrorIconPath;
    }

    /// <summary>
    /// 加载图标 Texture2D，找不到对应文件时自动使用 Error 占位图。
    /// </summary>
    public static Texture2D LoadIcon(this PowerCategory category, string iconId)
    {
        string path = ResolveIconPath(category, iconId);
        return ResourceLoader.Load<Texture2D>(path);
    }
}
