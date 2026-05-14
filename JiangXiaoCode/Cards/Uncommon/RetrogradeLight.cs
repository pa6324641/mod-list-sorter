using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.HoverTips;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class RetrogradeLight : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-RETROGRADE_LIGHT";

    // [STS2_API] 構造函數：cost=2(升級後1), 類型=技能, 稀有度=罕見, 目標=自身
    public RetrogradeLight() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<RetrogradeLightEnemy>(),
        HoverTipFactory.FromCard<RetrogradeLightAlly>()
    ];

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 此處若本體卡片描述需要根據等級變更（例如：選擇 2/3 張卡），可在此處實作
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保戰鬥狀態與擁有者存在
        if (CombatState == null || Owner is not Player player) return;

        // 1. 生成兩張 Token 卡片實體
        var enemyToken = CombatState.CreateCard<RetrogradeLightEnemy>(player);
        var allyToken = CombatState.CreateCard<RetrogradeLightAlly>(player);

        // [優化] 確保 Token 的數值與當前玩家等級掛鉤
        if (enemyToken is JiangXiaoCardModel jxEnemy) jxEnemy.UpdateStatsBasedOnRank();
        if (allyToken is JiangXiaoCardModel jxAlly) jxAlly.UpdateStatsBasedOnRank();

        var choices = new List<CardModel> { enemyToken, allyToken };

        // 2. 彈出二選一畫面 (Discovery 邏輯)
        // [STS2_API] 使用 CardSelectCmd 觸發選擇介面，這會暫停 OnPlay 的執行直到玩家做出選擇
        var selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            player
        );

        // 3. 處理選擇結果
        if (selectedCard != null)
        {
            // [STS2_Logic] 此處需要 await，因為它涉及邏輯數據的變更（將卡片加入牌組狀態）
            var result = await CardPileCmd.AddGeneratedCardToCombat(
                selectedCard,
                PileType.Hand,
                false,              
                CardPilePosition.Top 
            );

            // [修正點] 刪除 await。因為 PreviewCardPileAdd 回傳 void，僅負責播放視覺動畫。
            CardCmd.PreviewCardPileAdd(result);
        }
    }

    protected override void OnUpgrade()
    {
        // 能量消耗 2 -> 1
        EnergyCost.UpgradeBy(-1);
        
        // [規範] 即使當前 ApplyRankLogic 為空，仍建議呼叫以維持擴展性
        UpdateStatsBasedOnRank();
    }
}