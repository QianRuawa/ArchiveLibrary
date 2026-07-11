using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 防御贯穿：攻击时每层贯穿 1 点格挡（不消耗格挡），
/// 贯穿层数大于护盾层数时无视护盾。
/// 格挡穿透由 Harmony 补丁 <see cref="PenetrationBlockBypassPatch"/> 在 DamageBlockInternal 中实现。
/// </summary>
[RegisterPower]
public class PenetrationBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "Penetration";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>当前攻击中需要穿透的目标。</summary>
    internal static Creature? PendingTarget;
    /// <summary>当前攻击的穿透量。</summary>
    internal static decimal PendingAmount;

    /// <summary>供外部（如穿甲弹）设置穿透目标与量。</summary>
    public static void SetPending(Creature target, decimal amount)
    {
        PendingTarget = target;
        PendingAmount = amount;
    }

    /// <summary>清空穿透状态。</summary>
    public static void ClearPending()
    {
        PendingTarget = null;
        PendingAmount = 0;
    }

    /// <summary>
    /// 攻击前清空残留状态，再判断是否设置穿透。
    /// 注意：返回 0 不会被加入 modifiers 列表，AfterModifyingDamageAmount 不会触发。
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理自己的攻击
        if (dealer != Owner) return 0m;

        // 自己的攻击，清空残留并重新设置
        PendingTarget = null;
        PendingAmount = 0;

        if (!props.IsPoweredAttack()) return 0m;
        if (target == null || target.Block <= 0 || Amount <= 0)
            return 0m;

        PendingTarget = target;
        PendingAmount = Amount;
        //LibraryLogger.Info($"[贯穿] 设置穿透 PendingTarget={target.IsPlayer}({target.GetHashCode()}) Block={target.Block} Amount={Amount}");
        return 0m;
    }
}
