using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using ArchiveLibrary.Scripts.Utils;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 暴击率提升：增加单位的暴击率。
/// 暴击时造成双倍伤害（可通过 <see cref="CritDamageMultiplier"/> 修改）。
/// 暴击率受目标 <see cref="CriticalChanceResistModifier"/> 影响。
/// 暴击伤害受 <see cref="CriticalDamageModifier"/> 和 <see cref="CriticalDamageResistModifier"/> 影响。
/// </summary>
[RegisterPower]
public class CriticalChanceBenefit : ModPowerTemplate, IPowerCategorizable
{
    public string IconId => "CriticalChance";
    public PowerCategory Category => PowerCategory.Benefit;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    /// <summary>当前攻击是否暴击，供伤害数字补丁读取。</summary>
    internal bool WasCrit;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private decimal _multiplierPerStack = 0.05m;
    private decimal _critDamageMultiplier = 2.0m;

    public decimal MultiplierPerStack => _multiplierPerStack;
    public decimal CritDamageMultiplier
    {
        get => _critDamageMultiplier;
        set
        {
            _critDamageMultiplier = value;
            UpdateCritPercent();
            InvokeDisplayAmountChanged();
        }
    }

    public void SetMultiplierPerStack(decimal multiplier)
    {
        _multiplierPerStack = multiplier;
        UpdateTotalPercent();
        InvokeDisplayAmountChanged();
    }

    public override int DisplayAmount => (int)(Math.Abs(Amount) * _multiplierPerStack * 100);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TotalPercent", 0m),
        new DynamicVar("CritBonusPercent", 0m)
    ];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            UpdateTotalPercent();
            InvokeDisplayAmountChanged();
            Flash();
        }
    }

    /// <summary>刷新暴击率/暴击伤害显示，供 <see cref="CriticalDamageModifier"/> 等外部调用。</summary>
    public void RefreshCritDisplay()
    {
        UpdateTotalPercent();
        InvokeDisplayAmountChanged();
    }

    private void UpdateTotalPercent()
    {
        DynamicVars["TotalPercent"].BaseValue = DisplayAmount;
        UpdateCritPercent();
    }

    private void UpdateCritPercent()
    {
        decimal total = (_critDamageMultiplier - 1m) * 100;
        // 加上暴击伤害率修正
        if (Owner != null)
        {
            var dmgUp = Owner.GetPower<CriticalDamageModifier>();
            if (dmgUp != null)
                total += dmgUp.Amount * dmgUp.MultiplierPerStack * 100;
        }
        DynamicVars["CritBonusPercent"].BaseValue = (int)total;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        WasCrit = false; // 必须最先清除，防止上一次次暴击残留
        if (dealer != Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (target == null) return 1m;

        // 玩家卡牌预览时跳过暴击（不在执行卡牌/药水效果时即为预览）
        if (cardSource?.Owner != null)
        {
            var cm = CombatManager.Instance;
            if (cm != null && !cm.IsExecutingCardOrPotionEffect(cardSource.Owner))
            {
                LibraryLogger.Debug("暴击预览跳过：非执行卡牌效果中");
                return 1m;
            }
        }

        // 有效暴击率 = 己方暴击率 - 目标暴击抵抗
        decimal chance = Math.Max(0m, Amount * _multiplierPerStack);
        var resist = target.GetPower<CriticalChanceResistModifier>();
        if (resist != null)
            chance = Math.Max(0m, chance - resist.Amount * resist.MultiplierPerStack);

        if (chance <= 0m) return 1m;
        // 确定性种子：同一卡牌/攻击在同一回合内判定结果一致
        int seed = HashCode.Combine(
            CombatState?.RoundNumber ?? 0,
            cardSource?.GetHashCode() ?? target.GetHashCode(),
            dealer?.GetHashCode() ?? 0);
        if (new Random(seed).NextDouble() >= (double)Math.Min(chance, 1.0m))
            return 1m;

        // 暴击！计算倍率
        decimal multi = _critDamageMultiplier;

        var dmgUp = Owner.GetPower<CriticalDamageModifier>();
        if (dmgUp != null)
            multi += dmgUp.Amount * dmgUp.MultiplierPerStack;

        var dmgResist = target.GetPower<CriticalDamageResistModifier>();
        if (dmgResist != null)
            multi = Math.Max(1.0m, multi + dmgResist.Amount * dmgResist.MultiplierPerStack);

        WasCrit = true;
        LibraryLogger.Debug($"暴击触发！目标={target.GetHashCode()}, 卡牌={cardSource?.GetHashCode()}, 倍率={multi}");
        return multi;
    }

    /// <summary>暴击实际结算后闪烁图标（只在真正出伤时闪一次，预览/计算中不闪）。</summary>
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        if (WasCrit)
            Flash();
        return Task.CompletedTask;
    }
}
