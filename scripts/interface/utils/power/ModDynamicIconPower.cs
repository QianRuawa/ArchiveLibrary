using Godot;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 为需要在 Amount 正负变化时自动切换增益/减益图标的 Power 提供基类。
/// 标题、描述、图标路径、简介框颜色均根据 Amount 符号自动切换。
/// 子类只需实现抽象成员（Type、StackType、IconId、Category）和业务逻辑。
/// </summary>
public abstract class ModDynamicIconPower : ModPowerTemplate, IPowerCategorizable, IDynamicIconPower, IPowerExtraIconAmountLabelSpecsProvider
{
    // ===== IPowerCategorizable =====
    public abstract string IconId { get; }
    public abstract PowerCategory Category { get; }
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    // ===== IDynamicIconPower =====
    /// <summary>正数图标路径，默认从 Category + IconId 自动生成。可 override 自定义。</summary>
    public virtual string PositiveIconPath => Category.GetIconPath(IconId);
    /// <summary>负数图标路径，默认使用 Adverse 分类 + IconId 自动生成。可 override 自定义。</summary>
    public virtual string NegativeIconPath => PowerCategory.Adverse.GetIconPath(IconId);

    // ===== 通用配置 =====
    public override bool AllowNegative => true;

    // ===== 通用视觉 =====
    public override Color AmountLabelColor => Amount >= 0
        ? new Color("FFF6E2")  // cream
        : new Color("FF5555"); // red

    // ===== 标题/描述自动切换 =====
    public override LocString Title => Amount >= 0
        ? new LocString("powers", Id.Entry + ".title")
        : new LocString("powers", Id.Entry + ".title.negative");

    public override LocString Description => Amount >= 0
        ? new LocString("powers", Id.Entry + ".description")
        : new LocString("powers", Id.Entry + ".description.negative");

    // ===== 智能描述 =====
    // 正负共用 .smartDescription，用 {Amount:cond:>0?...|...} 条件式区分
    protected override string SmartDescriptionLocKey => base.Id.Entry + ".smartDescription";

    // ===== 额外角标（子类 override 添加） =====
    public virtual IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs() => [];
}
