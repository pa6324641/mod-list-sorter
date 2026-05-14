using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Common;

/// <summary>
/// 星力充沛：1費 技能 稀有
/// 效果：恢復能量 M 點。
/// 星級品質 1-2 (+1), 3-4 (+3), 5-6 (+5), 7 (+7)
/// 升級：能量消耗 -1
/// </summary>

[Pool(typeof(JiangXiaoCardPool))]
public class AbundantStar : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-ABUNDANT_STAR";

    public AbundantStar() : base(1, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        // 初始化時設定基礎消耗為 1
        JJCustomVar("M",1m);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJStaticTip(StaticHoverTip.Energy);
    }

    // protected override IEnumerable<DynamicVar> CanonicalVars => [
    //     new DynamicVar("M", 1m) // 初始值為 1，會由 UpdateStatsBasedOnRank 動態覆蓋
    // ];

    // [STS2_Compatibility] 關鍵字將顯示於 HoverTip
    // protected override IEnumerable<IHoverTip> ExtraHoverTips => [
    //     HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
    //     HoverTipFactory.Static(StaticHoverTip.Energy)
        
    // ];

    /// <summary>
    /// 獲取當前角色的星級品質等級
    /// </summary>
    private int GetQualityRank()
    {
        // 優先獲取卡片擁有者，若為空（如在圖鑑中）則從全局 RunManager 獲取首位玩家
        var player = Owner ?? RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
        var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.SkillRank ?? 1; // 預設 1 級
    }

    /// <summary>
    /// 根據當前星級品質更新變量 M
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {        
        // 根據規則：1-2 -> 1, 3-4 -> 3, 5-6 -> 5, 7 -> 7
        decimal calculatedM = skillRank switch
        {
            <= 2 => 1m,
            <= 4 => 3m,
            <= 6 => 5m,
            _ => 5m
        };

        DynamicVars["M"].BaseValue = calculatedM;
    }

    // public override Task BeforeCombatStart()
    // {
    //     UpdateStatsBasedOnRank();
    //     return base.BeforeCombatStart();
    // }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 💡 確保在打出時根據當前最新星級更新數值
        UpdateStatsBasedOnRank();
        
        int energyAmount = (int)DynamicVars["M"].BaseValue;

        if (Owner?.Creature != null && energyAmount > 0)
        {
            // [STS2_Compatibility] 使用 CreatureCmd.GainEnergy 增加能量
            await PlayerCmd.GainEnergy(energyAmount, this.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級效果：能量消耗 1 -> 0
        EnergyCost.UpgradeBy(-1);
    }
}