using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Localization.DynamicVars; // 必須引用以支援 DynamicVar

namespace JiangXiaoMod.Code.Powers;

public sealed class DomainOfTearsPower : JiangXiaoPowerModel
{
    public override PowerType Type => PowerType.Buff;
    // 注意：若希望數值能被更新，StackType 建議改為 Aggregate 
    // 但若不希望在圖示旁顯示數字，維持 None 並搭配 CanonicalVars 是最好的
    public override PowerStackType StackType => PowerStackType.None;

    private const string BasicStrikeId = "JIANGXIAOMOD-STRIKE_JIANG_XIAO";
    private const string BasicDefendId = "JIANGXIAOMOD-DEFEND_JIANG_XIAO";
    private const string BasicHealId = "JIANGXIAOMOD-BLESSING";
    
    private const string SpaceGaptId = "JIANGXIAOMOD-SPACE_GAP";
    private const string QingMangId = "JIANGXIAOMOD-QING_MANG";
    private const string StarPowerAbundantId = "JIANGXIAOMOD-ABUNDANT_STAR";

    // 用於儲存當前計算出的 X 值，供文本顯示
    private int _displayX = 1;

    public DomainOfTearsPower() : base() { }

    // ======================================================================
    // 🌟 文本動態變數設定：讓 powers.json 可以使用 {M}
    // ======================================================================
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("M", (decimal)_displayX)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.IsDead || CombatState == null) return;
        if (Owner != player.Creature) return; 

        // 1. 獲取「星技等級」並計算 X 值
        int starLevel = GetCurrentStarSkillLevel(player);
        int xValue = 1;
        if (starLevel >= 4 && starLevel <= 5) xValue = 2;
        else if (starLevel >= 6) xValue = 3;

        // 2. [修復 CS0200] 更新能力的數值
        // 更新本地顯示變數
        _displayX = xValue;
        // 透過 PowerCmd 更新實體的 Amount (這會同步到 UI 並處理疊加邏輯)
        await PowerCmd.Apply<DomainOfTearsPower>(Owner, xValue, Owner, null);

        // 3. 牌組檢索
        var deckCards = player.Deck.Cards;
        bool hasRift = deckCards.Any(c => c.Id.Entry == SpaceGaptId);
        bool hasQingMang = deckCards.Any(c => c.Id.Entry == QingMangId);
        bool hasStarPower = deckCards.Any(c => c.Id.Entry == StarPowerAbundantId);

        // 4. 分析敵方全體意圖
        bool isAnyAttacking = false;
        bool isAnyDefending = false;
        bool isAnyBuffDebuff = false;

        foreach (var enemy in CombatState.HittableEnemies)
        {
            var nextMove = enemy.Monster?.NextMove;
            if (nextMove == null || nextMove.Intents == null) continue;

            if (nextMove.Intents.Any(intent => intent is AttackIntent)) 
                isAnyAttacking = true;
            else if (nextMove.Intents.Any(intent => intent is DefendIntent)) 
                isAnyDefending = true;
            else 
                isAnyBuffDebuff = true;
        }

        // 5. 執行自動反制邏輯
        if (isAnyAttacking)
        {
            string tid = hasRift ? SpaceGaptId : BasicDefendId;
            await ProcessAutoPlay(player, tid, xValue, choiceContext);
        }

        if (isAnyDefending)
        {
            string tid = hasQingMang ? QingMangId : BasicStrikeId;
            await ProcessAutoPlay(player, tid, xValue, choiceContext);
        }

        if (isAnyBuffDebuff)
        {
            await ProcessAutoPlay(player, BasicHealId, xValue, choiceContext);
            if (hasStarPower)
            {
                await ProcessAutoPlay(player, StarPowerAbundantId, xValue, choiceContext);
            }
        }
    }

    private async Task ProcessAutoPlay(Player player, string cardId, int count, PlayerChoiceContext ctx)
    {
        for (int i = 0; i < count; i++)
        {
            var targetCard = PileType.Draw.GetPile(player).Cards.FirstOrDefault(c => c.Id.Entry == cardId)
                          ?? PileType.Exhaust.GetPile(player).Cards.FirstOrDefault(c => c.Id.Entry == cardId)
                          ?? PileType.Hand.GetPile(player).Cards.FirstOrDefault(c => c.Id.Entry == cardId);

            if (targetCard != null)
            {
                await CardCmd.AutoPlay(ctx, targetCard, ResolveTargetFor(targetCard));
            }
            else
            {
                break; 
            }
        }
    }

    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy && card.TargetType != TargetType.AllEnemies)
            return null;
        var enemies = CombatState.HittableEnemies.ToList();
        return enemies.Count == 0 ? null : Owner?.Player?.RunState?.Rng?.CombatTargets?.NextItem(enemies);
    }

    private int GetCurrentStarSkillLevel(Player player)
    {
        // 尋找遺物中的星技等級
        var starMap = player.Relics.OfType<JiangXiaoMod.Code.Relics.IInnerStarMap>().FirstOrDefault();
        if (starMap != null)
        {
            // 根據您的需求：1-3=1、4-5=2、6-7=3
            // 這裡假設等級存在星圖的某個屬性中，請根據實際屬性名修改
            return 1; 
        }
        return 1;
    }
}