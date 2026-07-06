using ArchiveLibrary.Scripts.Visual;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ArchiveLibrary.Scripts.Powers;

/// <summary>
/// 卡牌/非卡牌伤害免疫。
/// 控制台用 <see cref="Acquire(PlayerChoiceContext, Creature, bool, Creature?)"/>。
/// 代码中用 Apply 后调 <see cref="SetBlockNonCard"/>。
/// </summary>
[RegisterPower]
public class ImmuneDamageParticular : ModPowerTemplate, IPowerCategorizable
{
    /// <summary>一行获取：先配置模式再 Apply，确保通知显示正确标题。</summary>
    public static async Task<ImmuneDamageParticular?> Acquire(PlayerChoiceContext ctx, Creature target, bool blockNonCard, Creature? applier = null)
    {
        var power = (ImmuneDamageParticular)ModelDb.Power<ImmuneDamageParticular>().ToMutable();
        power.SetBlockNonCard(blockNonCard);
        await PowerCmd.Apply(ctx, power, target, 1, applier, null);
        return power;
    }
    private bool _nonCardMode = true;

    /// <summary>设置免疫模式。</summary>
    public void SetBlockNonCard(bool blockNonCard)
    {
        _nonCardMode = blockNonCard;
        if (DynamicVars != null && DynamicVars.TryGetValue("ImmuneType", out var dt))
            ((StringVar)dt).StringValue = ImmuneTypeText;
        Flash();
    }

    public bool IsNonCardImmune => _nonCardMode;

    public override int DisplayAmount => 1;

    private string ImmuneTypeText => EntiyArchiveLibrary.UI.GetText(_nonCardMode ? "IMMUNE_TYPE_NON_CARD" : "IMMUNE_TYPE_CARD") ?? "非卡牌";

    public virtual string IconId => "ImmuneDamage";
    public PowerCategory Category => PowerCategory.Particular;
    public override PowerAssetProfile AssetProfile => this.BuildAssetProfile();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("ImmuneType", ImmuneTypeText)
    ];

    public override LocString Title => _nonCardMode
        ? new LocString("powers", Id.Entry + ".title")
        : new LocString("powers", Id.Entry + ".title.negative");

    public override LocString Description => new LocString("powers", Id.Entry + ".description");

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
            return amount;

        bool fromPlayer = dealer != null && dealer.Side == CombatSide.Player;

        // 非卡牌免疫模式 → 阻挡非玩家伤害（怪物/环境）
        // 卡牌免疫模式 → 阻挡玩家伤害（卡牌）
        if (_nonCardMode && !fromPlayer)
        {
            Flash();
            NImmuneVfx.Display(Owner, EntiyArchiveLibrary.UI.GetText("IMMUNE_DAMAGE"));
            return 0m;
        }
        if (!_nonCardMode && fromPlayer)
        {
            Flash();
            NImmuneVfx.Display(Owner, EntiyArchiveLibrary.UI.GetText("IMMUNE_DAMAGE"));
            return 0m;
        }
        return amount;
    }
}
