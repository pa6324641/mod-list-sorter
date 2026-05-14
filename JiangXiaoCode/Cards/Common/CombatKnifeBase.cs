using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class CombatKnifeBase : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_BASE";
    private const string VarM = "M"; 

    public CombatKnifeBase() : base(2, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        
        // 初始化動態變數 M，預設值為 1
        JJCustomVar(VarM, 1m);
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<CombatKnifeSlash>(),
        HoverTipFactory.FromCard<CombatKnifeStab>()
    ];

    protected override void OnUpgrade()
    {
        // 升級時將變數 M 改為 2
        DynamicVars[VarM].BaseValue = 2m;
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        int count = (int)DynamicVars[VarM].BaseValue;

        for (int i = 0; i < count; i++)
        {
            // 1. 建立 Token 選項
            var choices = new List<CardModel>
            {
                CombatState.CreateCard<CombatKnifeSlash>(Owner),
                CombatState.CreateCard<CombatKnifeStab>(Owner)
            };

            // 2. 如果主卡已升級，則將所有 Token 選項也升級
            if (IsUpgraded)
            {
                foreach (var token in choices)
                {
                    token.UpgradeInternal();
                }
            }

            // 3. 彈出二選一畫面
            var selectedCard = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                choices,
                Owner
            );

            if (selectedCard != null)
            {
                // 4. 將選擇的牌加入手牌 (若主卡升級，此處拿到的 selectedCard 已經是升級版)
                await CardPileCmd.AddGeneratedCardToCombat(
                    selectedCard,
                    PileType.Hand,
                    true, 
                    CardPilePosition.Top 
                );

                // 5. 同時將副本加入消耗堆 (CreateClone 會保留升級狀態)
                var copyForExhaust = selectedCard.CreateClone();
                // copyForExhaust.AddKeyword(CardKeyword.Exhaust);
                await CardPileCmd.Add(
                    copyForExhaust,
                    PileType.Exhaust
                );
                
                selectedCard.InvokeExecutionFinished();
            }
        }
    }
}