using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class SecondaryFace : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-Secondary_Face";

    public SecondaryFace() : base(3, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJCustomVar("M", 1m);
        JJKeywordAndTip(CardKeyword.Exhaust);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null) return;
        decimal mValue = skillRank switch { >= 6 => 3m, >= 4 => 2m, _ => 1m };

        if (DynamicVars.ContainsKey("M"))
        {
            DynamicVars["M"].BaseValue = mValue;
            DynamicVars["M"].PreviewValue = mValue; 
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var combat = player?.Creature?.CombatState;
        if (player == null || combat == null) return;

        UpdateStatsBasedOnRank();
        int copyCount = (int)DynamicVars["M"].BaseValue;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = false, 
            Cancelable = false                
        };

        // 開啟選牌介面
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext, 
            player, 
            prefs, 
            (card) => card.Id != this.Id, 
            this
        );

        CardModel? targetCard = selectedCards?.FirstOrDefault();
        if (targetCard == null) return;

        for (int i = 0; i < copyCount; i++)
        {
            // 1. [官方 API] 完美複製卡牌實例，保留所有戰鬥中的臨時狀態、升級與附魔
            CardModel copy = targetCard.CreateClone();

            // 2. 設定複製品專有屬性：0費
            copy.EnergyCost.SetCustomBaseCost(0);

            // 3. [官方 API] 使用 CardCmd 正確賦予「消耗」詞條，確保系統和 UI 正確響應
            CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);

            // 4. [效果重放] 刷新江曉專屬數值，確保星技品質正確套用
            if (copy is JiangXiaoCardModel jxCard)
            {
                jxCard.UpdateStatsBasedOnRank();
            }

            // 5. 刷新附魔顯示數值
            copy.Enchantment?.RecalculateValues();

            // 6. 加入戰鬥手牌
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, addedByPlayer: true);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}