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
public sealed class DefendJiangXiao : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-DEFEND_JIANG_XIAO";
    private const decimal BaseBlock = 5m;
    private const decimal UpgradeBlock = 3m;
    private const decimal RankBonus = 2m;

    public DefendJiangXiao() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        JJTag(CardTag.Defend);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJStaticTip(StaticHoverTip.Block);
        JJBlock(BaseBlock,ValueProp.Move);
    }

    // STS2 屬性：標記此卡會獲得格擋，利於 AI 識別與 UI 顯示
    // public override bool GainsBlock => true;

    // protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    
    // // 必須包含星技關鍵字
    // public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.Star];

    // // 初始化基礎格擋變量
    // protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BaseBlock, ValueProp.Move)];

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 核心邏輯：根據當前星技品質等級更新格擋基礎值。
    /// 公式：(基礎 5 + 升級 3) + (等級 - 1) * 3
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 安全檢查：確保 DynamicVars 已經被系統初始化
        if (DynamicVars?.Block == null) return;

        // 調用 JiangXiaoUtils 獲取遺物提供的品質等級 (1-7)
        // int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal currentBase = IsUpgraded ? (BaseBlock + UpgradeBlock) : BaseBlock;
        
        // 更新 BaseValue 會觸發 STS2 的 LocString 自動重繪 UI 數值
        DynamicVars.Block.BaseValue = currentBase + (skillRank - 1) * RankBonus;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出時再次刷新，防止極端情況下的數值落後
        UpdateStatsBasedOnRank();

        // 執行格擋指令：作用於玩家自己，使用更新後的 DynamicVars.Block
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        // 升級時降低消耗，數值更新交給 UpdateStatsBasedOnRank 統一處理
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank(); 
    }
}