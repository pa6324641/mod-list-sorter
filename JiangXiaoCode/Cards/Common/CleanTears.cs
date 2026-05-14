using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players; 
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Runs;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class CleanTears : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-CLEAN_TEARS";
    // public const string M = "M";

    public CleanTears() : base(2, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJCustomVar("M", 1m);
    }

    // protected override IEnumerable<DynamicVar> CanonicalVars => [
    //     new DynamicVar("M", 1m)
    // ];

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        decimal mValue = skillRank switch
        {
            >= 6 => 5m,
            >= 4 => 3m,
            _ => 1m
        };
        DynamicVars["M"].BaseValue = mValue;
    }

 	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null) return;

        int mLimit = (int)DynamicVars["M"].BaseValue;

        // --- 遍歷戰鬥中的所有玩家 (支援多人/友方) ---
        foreach (var playerEntity in combat.Players)
        {
            // [修正] Player 模型不直接持有 Powers，需訪問其 Creature 實體
            // 同時加上 null 檢查以確保安全
            if (playerEntity.Creature != null)
            {
                // 1. 移除該玩家生物實體身上所有的 Debuff
                var debuffs = playerEntity.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
                foreach (var debuff in debuffs)
                {
                    // PowerCmd.Remove 通常需要指定目標生物或直接傳入能力實例
                    await PowerCmd.Remove(debuff);
                }
            }

            // 2. 獲取該玩家的戰鬥狀態以訪問牌堆 (這個部分原先就是正確的)
            var pCombatState = playerEntity.PlayerCombatState;
            if (pCombatState == null) continue;

            // 定義內部邏輯來處理該玩家的牌堆
            async Task PurgePlayerPile(CardPile pile)
            {
                var toPurge = pile.Cards
                    .Where(c => c.Type == CardType.Status || c.Type == CardType.Curse)
                    .Take(mLimit)
                    .ToList();

                if (toPurge.Count > 0)
                {
                    //跑動畫移到消耗堆
                    await CardPileCmd.Add(toPurge, PileType.Exhaust, CardPilePosition.Bottom, this);
                    //直接移除
                    // await CardPileCmd.RemoveFromCombat(toPurge);
                }
            }

            // 分別淨化該玩家的手牌、抽牌堆、棄牌堆
            await PurgePlayerPile(pCombatState.Hand);
            await PurgePlayerPile(pCombatState.DrawPile);
            await PurgePlayerPile(pCombatState.DiscardPile);
        }
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}