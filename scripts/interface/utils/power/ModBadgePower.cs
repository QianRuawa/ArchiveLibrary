using Godot;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 带额外角标的 Power 基类。
/// 继承此类的 Power 自动获得 <see cref="IPowerExtraIconAmountLabelSpecsProvider"/> 支持。
/// 子类只需 override <see cref="GetPowerBadgeSpecs"/> 返回角标列表。
///
/// 默认颜色配置可通过静态属性全局修改：
/// <code>
/// ModBadgePower.DefaultFontColor = Colors.Gold;
/// ModBadgePower.DefaultOutlineColor = Colors.Black;
/// </code>
///
/// 单个 Power 可在 GetPowerBadgeSpecs 中自定义：
/// <code>
/// new ExtraIconAmountLabelSpec(text, corner, default, myFontColor, myOutlineColor)
/// </code>
/// </summary>
public abstract class ModBadgePower : ModPowerTemplate, IPowerCategorizable, IPowerExtraIconAmountLabelSpecsProvider
{
    // ===== IPowerCategorizable =====
    public abstract string IconId { get; }
    public abstract PowerCategory Category { get; }
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    // ===== 角标默认颜色（可全局修改） =====
    public static Color? DefaultFontColor { get; set; }
    public static Color? DefaultOutlineColor { get; set; }

    // ===== 角标规格（子类 override） =====
    protected abstract IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerBadgeSpecs();

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs() => GetPowerBadgeSpecs();

    // ===== 便捷创建方法 =====

    /// <summary>创建普通角标，带默认颜色。</summary>
    protected static ExtraIconAmountLabelSpec MakeBadge(ExtraIconAmountLabelCorner corner, string text)
    {
        return new ExtraIconAmountLabelSpec(text, corner, default, DefaultFontColor, DefaultOutlineColor);
    }

    /// <summary>创建富文本角标（支持 BBCode 颜色）。</summary>
    protected static ExtraIconAmountLabelSpec MakeRichBadge(ExtraIconAmountLabelCorner corner, string richText)
    {
        return ExtraIconAmountLabelSpec.RichText(corner, richText);
    }

    /// <summary>创建完全自定义角标。</summary>
    protected static ExtraIconAmountLabelSpec MakeCustomBadge(ExtraIconAmountLabelCorner corner, string text, Color? fontColor, Color? outlineColor)
    {
        return new ExtraIconAmountLabelSpec(text, corner, default, fontColor, outlineColor);
    }
}
