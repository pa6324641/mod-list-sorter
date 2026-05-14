using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeHundredRips : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_HUNDRED_RIPS";
    private const string VarX = "X";

    public CombatKnifeHundredRips() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        
        JJCustomVar(VarX, 0m); // 次數 X[cite: 15]
        JJDamage(6m);          // 基礎傷害 M[cite: 15]
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null || DynamicVars == null) return;

        int knifeRank = JiangXiaoUtils.GetCombatKnifeRank(player);
        int unarmedRank = JiangXiaoUtils.GetUnarmedRank(player);
        
        int xHits = knifeRank + unarmedRank;
        DynamicVars[VarX].BaseValue = xHits / 2;
        // M 為 基礎6 + X[cite: 15]
        DynamicVars.Damage.BaseValue = 6m + xHits;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if(cardPlay.Target == null) return;
        int hits = (int)DynamicVars[VarX].BaseValue;
        for (int i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }
}