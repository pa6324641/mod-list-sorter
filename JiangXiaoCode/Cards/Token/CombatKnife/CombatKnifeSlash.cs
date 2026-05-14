using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeSlash : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_SLASH";
    private const decimal BaseDmg = 9m;
    private const decimal RankScaling = 2m; // 每級成長傷害

    public CombatKnifeSlash() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        JJDamage(BaseDmg, ValueProp.Move);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (DynamicVars?.Damage == null) return;
        
        // 根據「格鬥刀精通」等級進行加成
        int knifeRank = JiangXiaoUtils.GetCombatKnifeRank(player);
        DynamicVars.Damage.BaseValue = BaseDmg + (knifeRank - 1) * RankScaling;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}