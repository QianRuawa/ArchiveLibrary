using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 让 Power 声明自己的分类和图标 ID，自动解析图标路径和 AssetProfile。
/// </summary>
public interface IPowerCategorizable
{
    /// <summary>所属分类（Benefit / Adverse / CrowdControl / Particular）</summary>
    PowerCategory Category { get; }

    /// <summary>图标 ID 标识，如 "ATK"、"DEF"、"Bleed" 等</summary>
    string IconId { get; }
}

public static class PowerCategorizableExtensions
{
    /// <summary>
    /// 自动生成图标完整路径。
    /// 等效于调用 <c>Category.GetIconPath(IconId)</c>。
    /// </summary>
    public static string GetIconPath(this IPowerCategorizable power)
    {
        return power.Category.GetIconPath(power.IconId);
    }

    /// <summary>
    /// 根据分类和 IconId 自动构建 <see cref="PowerAssetProfile"/>，
    /// 免去手动写 AssetProfile override。
    /// <para>用法：<c>public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();</c></para>
    /// </summary>
    public static PowerAssetProfile BuildAssetProfile(this IPowerCategorizable power)
    {
        string path = power.GetIconPath();
        return new PowerAssetProfile(IconPath: path, BigIconPath: path);
    }
}
