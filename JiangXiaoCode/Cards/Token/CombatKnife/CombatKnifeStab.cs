using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.Entities.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.Powers; // 引用易傷能力

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeStab : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_STAB";
    private const decimal BaseDmg = 6m;
    private const decimal RankScaling = 1m;
    private const string VarVulnerable = "X";

    public CombatKnifeStab() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        JJDamage(BaseDmg, ValueProp.Move);
        JJCustomVar(VarVulnerable, 2m);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (DynamicVars?.Damage == null) return;
        
        int knifeRank = JiangXiaoUtils.GetCombatKnifeRank(player);
        DynamicVars.Damage.BaseValue = BaseDmg + (knifeRank - 1) * RankScaling;
        
        // 易傷層數隨 Rank 稍微成長 (每 3 級 +1)
        DynamicVars[VarVulnerable].BaseValue = 2m + ((knifeRank - 1) / 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        // 1. 造成傷害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 給予易傷
        // [STS1_Legacy] 說明：此處邏輯參考自 STS1 VulnerablePower，需確認 STS2 是否有對應組件
        // [注意] 若手冊未定義易傷類名，此處預設為 VulnerablePower
        int vLayer = (int)DynamicVars[VarVulnerable].BaseValue;
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, vLayer, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars[VarVulnerable].UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}