using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeForbiddenZone : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_FORBIDDEN_ZONE";

    public CombatKnifeForbiddenZone() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null || Owner == null) return;

        // 1. 移除敵方 1 個 Buff (尋找第一個 Buff 類型能力)[cite: 16]
        var buffToRemove = target.Powers.FirstOrDefault(p => p.Type == PowerType.Buff && p.Id.Entry != "SWIPE_POWER");
        if (buffToRemove != null) await PowerCmd.Remove(buffToRemove);

        // 2. 移除敵方全部護甲[cite: 16]
        if (target.Block > 0) await CreatureCmd.LoseBlock(target, target.Block);

        // 3. 手中的「格鬥刀」攻擊牌自動打出[cite: 16]
        if (Owner.PlayerCombatState == null) return;
        var handAttackKnives = Owner.PlayerCombatState.Hand.Cards
            .Where(c => c.IsJiangXiaoModCOMBATKNIFE() && c.Type == CardType.Attack)
            .ToList();

        foreach (var card in handAttackKnives)
        {
            // 使用前面實作過的隨機目標選擇邏輯[cite: 16]
            await CardCmd.AutoPlay(choiceContext, card, target); 
        }
    }
}