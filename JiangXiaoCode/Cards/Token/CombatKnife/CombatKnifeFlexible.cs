using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeFlexible : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_FLEXIBLE";
    // 定義動態變數標籤，對應 Localization.json 中的 !X!
    private const string VarX = "X";

    public CombatKnifeFlexible() : base(3, CardType.Skill, CardRarity.Token, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        
        // 初始化動態變數 X
        JJCustomVar(VarX, 1m);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null || DynamicVars == null) return;

        // 計算 格鬥刀rank + 徒手格鬥rank /2 並更新至動態文本
        int knifeRank = JiangXiaoUtils.GetCombatKnifeRank(player);
        // int unarmedRank = JiangXiaoUtils.GetUnarmedRank(player);
        DynamicVars[VarX].BaseValue = knifeRank / 2;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.PlayerCombatState == null) return;

        // 直接從已計算好的動態變數中取得數量
        int xCount = (int)DynamicVars[VarX].BaseValue;

        // 效果 3：從消耗堆選擇 X 張「格鬥刀」卡牌打出
        var knifeCardsInExhaust = Owner.PlayerCombatState.ExhaustPile.Cards
            .Where(c => c.IsJiangXiaoModCOMBATKNIFE() && c.Id != this.Id && c.Id.Entry != "JIANGXIAOMOD-COMBAT_KNIFE_ADVANCED" && c.Id.Entry != "JIANGXIAOMOD-COMBAT_KNIFE_BASE" && c.Id.Entry != "JIANGXIAOMOD-COMBAT_KNIFE_EXPERT")
            .ToList();

        var toHandPrompt = new LocString("card_selection", "TO_PLAY");
        var prefs = new CardSelectorPrefs(toHandPrompt, 1, xCount);
        if (knifeCardsInExhaust.Any())
        {
            int count = Math.Min(2, knifeCardsInExhaust.Count);
            var cardsToPlay = await CardSelectCmd.FromSimpleGrid(
                choiceContext, 
                knifeCardsInExhaust, 
                Owner, 
                prefs
            );

            foreach (var card in cardsToPlay)
            {
                // 自動打出且不消耗能量
                await CardCmd.AutoPlay(choiceContext, card, ResolveTargetFor(card));
            }
        }
    }
    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy || CombatState == null)
            return null;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return null;

        return Owner?.RunState?.Rng?.CombatTargets?.NextItem(enemies);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[VarX].UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}