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
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class CombatKnifeExpert : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_EXPERT";
    private const string VarM = "M"; 

    public CombatKnifeExpert() : base(5, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        JJCustomVar(VarM, 1m);
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<CombatKnifeHundredRips>(),
        HoverTipFactory.FromCard<CombatKnifeForbiddenZone>()
    ];

    // 當卡牌進入戰鬥時（例如戰鬥開始、被加入手牌、被抽到時）計算歷史記錄
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this || base.IsClone)
        {
            return Task.CompletedTask;
        }

        // 計算本場戰鬥 (This Combat) 中已使用的格鬥刀或徒手格鬥卡牌
        int amount = CombatManager.Instance.History.CardPlaysFinished.Count(e => 
            (e.CardPlay.Card.IsJiangXiaoModCOMBATKNIFE() || e.CardPlay.Card.IsJiangXiaoModUNARMED()) && 
            e.CardPlay.Card.Owner == base.Owner
        );
        // if(Owner.PlayerCombatState != null)
        // {
        //     amount += Owner.PlayerCombatState.ExhaustPile.Cards.Count(e => e.IsJiangXiaoModCOMBATKNIFE());
        // }
        

        ReduceCostBy(amount);
        return Task.CompletedTask;
    }
    // 修正點 2：監聽手牌中的即時變化
    public override Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        // 計算本場戰鬥 (This Combat) 中已使用的格鬥刀或徒手格鬥卡牌
        int amount = CombatManager.Instance.History.CardPlaysFinished.Count(e => 
            (e.CardPlay.Card.IsJiangXiaoModCOMBATKNIFE() || e.CardPlay.Card.IsJiangXiaoModUNARMED()) && 
            e.CardPlay.Card.Owner == base.Owner
        );
        // if(Owner.PlayerCombatState != null)
        // {
        //     amount += Owner.PlayerCombatState.ExhaustPile.Cards.Count(e => e.IsJiangXiaoModCOMBATKNIFE());
        // }
        

        ReduceCostBy(amount);
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        // Token 選擇邏輯
        var choices = new List<CardModel>
        {
            CombatState.CreateCard<CombatKnifeHundredRips>(Owner),
            CombatState.CreateCard<CombatKnifeForbiddenZone>(Owner)
        };

        if (this.IsUpgraded) foreach (var c in choices) c.UpgradeInternal();

        var selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner);
        if (selectedCard != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, true);
            var copyForExhaust = selectedCard.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copyForExhaust, PileType.Exhaust, false);
        }
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
    }

    private void ReduceCostBy(int amount)
    {
        base.EnergyCost.AddThisCombat(-amount);
    }
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}