using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.HoverTips;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class StarConceal : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-STAR_CONCEAL";

    public StarConceal() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJStaticTip(StaticHoverTip.Energy);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("E", 2m)
    ];

    // [STS2_Critical] 修正：Owner 是 Creature，必須透過 .Player 訪問遺物
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        // int rank = relic?.SkillRank ?? 1;

        decimal energyAmount = 2m;
        if (skillRank >= 6) energyAmount = 6m;
        else if (skillRank >= 4) energyAmount = 4m;

        DynamicVars["E"].BaseValue = energyAmount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null || Owner == null) return;

        UpdateStatsBasedOnRank();
        
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int rank = relic?.SkillRank ?? 1;
        int energyToGain = (int)DynamicVars["E"].BaseValue;

        if (rank >= 5)
        {
            // [STS2_Compatibility] 修正：遍歷 Allies 並檢查其 Player 屬性
            foreach (var ally in combat.Allies)
            {
                // 注意：ally 是 Creature，透過 ally.Player 取得 Player 模型
                if (ally.Player is Player playerModel)
                {
                    // 修正：GainEnergy 移除 cardPlay 參數
                    await PlayerCmd.GainEnergy( energyToGain, playerModel);
                }
            }
        }
        else
        {
            // 修正：cardPlay.Target 是 Creature，需透過 .Player 判定
            if (cardPlay.Target?.Player is Player playerTarget)
            {
                await PlayerCmd.GainEnergy(energyToGain, playerTarget);
            }
        }
    }
}