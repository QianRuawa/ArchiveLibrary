using ArchiveLibrary.Scripts.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.HoverTips;

/// <summary>
/// 智能悬浮提示工厂。
/// 解决 HoverTipFactory.FromPower&lt;T&gt;() 只能显示模板状态的问题。
/// 支持双面 Buff/Debuff、多模式 Power 等。
/// 卡牌提示框使用的是简单介绍（.description），不是 smartDescription。
/// </summary>
public static class SmartHoverTip
{
	/// <summary>费用减少模式简写。</summary>
	public enum CostMode { Half, Specific }

	/// <summary>
	/// 创建 Power 的悬浮提示（简单介绍）。
	/// Modifier 类型用 <paramref name="isDebuff"/> 切换 Debuff 方向的标题和描述。
	/// </summary>
	public static IHoverTip FromPower<T>(bool isDebuff = false) where T : PowerModel
	{
		var model = ModelDb.Power<T>().ToMutable();
		if (isDebuff)
		{
			// 通过反射直接设 _amount 绕过 Owner 检查
			var field = typeof(PowerModel).GetField("_amount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (field != null)
				field.SetValue(model, -1);
		}
		return model.GetDumbHoverTip();
	}

	/// <summary>
	/// 为 ExCostReductionBenefit 创建悬浮提示。
	/// <paramref name="mode"/> 用 <see cref="CostMode"/> 简写：
	///   Half → 普通减半，Specific → 指定卡牌减费。
	///   卡牌提示框用的是简单介绍（.description），不含卡牌名。
	/// </summary>
	public static IHoverTip FromCostReduction(CostMode mode = CostMode.Half)
	{
		var model = (ExCostReductionBenefit)ModelDb.Power<ExCostReductionBenefit>().ToMutable();
		model.Mode = mode == CostMode.Specific
			? ExCostReductionBenefit.ReduceMode.SpecificReduction
			: ExCostReductionBenefit.ReduceMode.NormalHalf;
		model.SetAmount(1, silent: true);

		if (mode == CostMode.Specific)
		{
			var sv = model.DynamicVars["ModeDesc"] as StringVar;
			if (sv != null)
				sv.StringValue = EntiyArchiveLibrary.UI.GetText("COST_REDUCTION_SPECIFIC_SIMPLE") ?? "被指定卡牌费用减少[blue]等量[/blue]点";
		}
		return model.GetDumbHoverTip();
	}

	/// <summary>批量合并多个悬浮提示。</summary>
	public static IEnumerable<IHoverTip> Many(params IHoverTip[] tips) => tips;
}
