using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class StrikeJiangXiao : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-STRIKE_JIANG_XIAO";
    private const decimal BaseDmg = 6m;
    private const decimal UpgradeDmg = 3m;
    private const decimal RankBonus = 2m;

    // 構造函數保持純淨，只定義基礎屬性
    public StrikeJiangXiao() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        JJTag(CardTag.Strike);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJDamage(BaseDmg, ValueProp.Move);
    }

    // protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    // public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.Star];
    
    // 這裡定義的 6m 是最基礎的顯示數值
    // protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BaseDmg, ValueProp.Move)];
    
    /// <summary>
    /// 核心邏輯：計算當前品質等級並修改基礎值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 安全檢查：確保 DynamicVars 已經被系統初始化
        if (DynamicVars?.Damage == null) return;

        // int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal currentBase = IsUpgraded ? (BaseDmg + UpgradeDmg) : BaseDmg;
        
        DynamicVars.Damage.BaseValue = currentBase + (skillRank - 1) * RankBonus;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        UpdateStatsBasedOnRank();

        // 這裡確保抓取的是更新後的 BaseValue
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升級時只修改能量，數值更新交給 UpdateStatsBasedOnRank
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank(); 
    }
}