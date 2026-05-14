using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class ShadowOfVoid : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-SHADOW_OF_VOID";

    public ShadowOfVoid() : base(4, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
        JJCustomVar("M", 1m); 
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJStaticTip(StaticHoverTip.Energy);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<RestToken>(),
        HoverTipFactory.FromCard<StrengthenToken>()
    ];

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        decimal calculatedM = skillRank switch
        {
            <= 3 => 1m,
            <= 5 => 2m,
            _ => 3m
        };

        if (DynamicVars.TryGetValue("M", out var mVar))
        {
            mVar.BaseValue = calculatedM;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null) return;

        // 更新動態數值
        UpdateStatsBasedOnRank();
        decimal energyAmount = DynamicVars.TryGetValue("M", out var mVar) ? mVar.BaseValue : 1m;

        // 建立待選的 Token 實例（此時它們尚未進入任何牌堆）
        var tokenStrengthen = CombatState.CreateCard<StrengthenToken>(Owner);
        var tokenRest = CombatState.CreateCard<RestToken>(Owner);

        // 1. 執行二選一介面
        var selectedToken = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, 
            new List<CardModel> { tokenStrengthen, tokenRest }, 
            Owner
        );

        if (selectedToken != null)
        {
            // 2. 將選中的實體卡片加入「發動者(Owner)」的手牌
            await CardPileCmd.AddGeneratedCardsToCombat(
                new List<CardModel> { selectedToken }, 
                PileType.Hand, 
                true 
            );

            // 3. 處理盟友：為每位盟友生成一個「全新的卡片實例」
            foreach (var allyCreature in CombatState.Allies)
            {
                // 排除自己，並確保盟友有對應的 Player 控制者
                if (allyCreature?.Player != null && allyCreature.Player != Owner)
                { 
                    // [重點修正]：根據選擇的類型，為盟友創建新的實例
                    CardModel allyTokenInstance;
                    if (selectedToken is StrengthenToken)
                    {
                        allyTokenInstance = CombatState.CreateCard<StrengthenToken>(allyCreature.Player);
                    }
                    else
                    {
                        allyTokenInstance = CombatState.CreateCard<RestToken>(allyCreature.Player);
                    }
                    
                    // 將新實例加入盟友手牌
                    await CardPileCmd.AddGeneratedCardsToCombat(
                        new List<CardModel> { allyTokenInstance }, 
                        PileType.Hand, 
                        false 
                    );
                }
            }

            // 4. 獲得能量 (轉為 int 確保符合 API 規範)
            await PlayerCmd.GainEnergy((int)energyAmount, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank();
    }
}