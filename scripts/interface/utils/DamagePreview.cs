using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ArchiveLibrary.Scripts.Damage;

/// <summary>
/// 通用伤害计算工具。模组制作者可调用 <see cref="Calculator"/> 的方法来计算实际伤害，
/// 或使用 <see cref="CardPreviewVar"/> 在卡牌预览中动态显示伤害值（含附魔、Power 等加成）。
/// 附魔类默认自动兼容原版及任意 mod 附魔。
/// </summary>
public static class Calculator
{
    /// <summary>估算一张卡牌对目标的最终伤害预览（战斗中真实计算，战斗外仅附魔）。</summary>
    public static int PreviewDamage(CardModel card, Creature? attacker, Creature? target, decimal baseDamage, ValueProp props)
    {
        if (CombatManager.Instance?.IsInProgress == true && attacker != null)
        {
            decimal modified = GetModifiedDamage(attacker, target, baseDamage, props, card);
            return (int)Math.Floor(modified);
        }
        else
        {
            return (int)Math.Floor(GetEnchantDamageBonus(card?.Enchantment, baseDamage, props));
        }
    }

    /// <summary>遍历攻击者和目标的全部 Power，计算伤害加成。</summary>
    public static decimal GetModifiedDamage(Creature? attacker, Creature? target, decimal baseDamage, ValueProp props, CardModel? cardSource)
    {
        decimal additive = 0m;
        decimal multiplicative = 1m;

        if (attacker != null)
        {
            foreach (var power in attacker.Powers)
            {
                additive += power.ModifyDamageAdditive(target, baseDamage, props, attacker, cardSource);
                multiplicative *= power.ModifyDamageMultiplicative(target, baseDamage, props, attacker, cardSource);
            }
        }

        if (target != null)
        {
            foreach (var power in target.Powers)
            {
                additive += power.ModifyDamageAdditive(target, baseDamage, props, attacker, cardSource);
                multiplicative *= power.ModifyDamageMultiplicative(target, baseDamage, props, attacker, cardSource);
            }
        }

        // 附魔加成
        if (cardSource?.Enchantment != null && cardSource.Enchantment.Status == EnchantmentStatus.Normal)
        {
            additive += cardSource.Enchantment.EnchantDamageAdditive(baseDamage, props);
            multiplicative *= cardSource.Enchantment.EnchantDamageMultiplicative(baseDamage, props);
        }

        decimal total = (baseDamage + additive) * multiplicative;

        // 伤害上限
        if (target != null)
        {
            decimal cap = decimal.MaxValue;
            foreach (var power in target.Powers)
            {
                var newCap = power.ModifyDamageCap(target, props, attacker, cardSource);
                cap = Math.Min(cap, newCap);
            }
            total = Math.Min(total, cap);
        }

        return total;
    }

    /// <summary>计算附魔对基础伤害的加成。</summary>
    public static decimal GetEnchantDamageBonus(EnchantmentModel? enchantment, decimal baseDamage, ValueProp props = ValueProp.Unpowered)
    {
        if (enchantment == null || enchantment.Status != EnchantmentStatus.Normal)
            return baseDamage;

        decimal add = enchantment.EnchantDamageAdditive(baseDamage, props);
        decimal mul = enchantment.EnchantDamageMultiplicative(baseDamage, props);
        return (baseDamage + add) * mul;
    }
}

/// <summary>
/// 卡牌预览伤害变量。战斗内完整计算（Power × 附魔），战斗外仅附魔。
/// 子类可重写 <see cref="GetCustomBonus"/> 添加自定义加成（如弹药、隐匿等）。
/// </summary>
public class CardPreviewVar : DamageVar
{
    public CardPreviewVar(decimal baseDamage, ValueProp props) : base(baseDamage, props) { }

    /// <summary>子类可重写此方法添加额外的倍率/加值。</summary>
    protected virtual (decimal multiplier, decimal addend) GetCustomBonus(CardModel card, Creature? attacker) => (1m, 0m);

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        decimal raw = BaseValue;
        var attacker = card.Owner?.Creature;

        if (CombatManager.Instance?.IsInProgress == true && attacker != null)
        {
            var (mul, add) = GetCustomBonus(card, attacker);
            decimal afterBonus = raw * mul + add;
            base.PreviewValue = (int)Math.Floor(Calculator.GetModifiedDamage(attacker, target, afterBonus, Props, card));
        }
        else
        {
            base.PreviewValue = (int)Math.Floor(Calculator.GetEnchantDamageBonus(card.Enchantment, raw, Props));
        }
    }
}

/// <summary>修复 Instinct 附魔：EnchantDamageMultiplicative 应返回 2 而非 1。</summary>
[HarmonyPatch(typeof(Instinct), nameof(Instinct.EnchantDamageMultiplicative))]
public static class InstinctDamageFixPatch
{
    public static bool Prefix(ref decimal __result)
    {
        __result = 2m;
        return false;
    }
}
