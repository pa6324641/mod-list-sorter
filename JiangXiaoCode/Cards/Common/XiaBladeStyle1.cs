using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions; 
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyle1 : JiangXiaoCardModel
{
    // 建議定義 CardId 確保與語系檔對齊
    public const string CardId = "JIANGXIAOMOD-XIA_BLADE_STYLE1";

    public XiaBladeStyle1() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBLADE);
        JJDamage(1m, ValueProp.Move);
        JJBlock(1m, ValueProp.Move);

        JJTag(CardTag.Strike);
    }
    /// <summary>
    /// 核心邏輯：根據「刀法等級 (BladeRank)」與「是否升級」計算數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取當前刀法等級
        int rank = JiangXiaoUtils.GetBladeRank(player);

        // 使用【三元運算子】決定基礎值：升級後基礎值為 2，否則為 1
        decimal currentBase = IsUpgraded ? 2m : 1m;

        // 最終數值 = 基礎值 + 等級加成 (每級 +1)
        decimal finalValue = currentBase + (rank * 1m);

        DynamicVars.Damage.BaseValue = finalValue;
        DynamicVars.Block.BaseValue = finalValue;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 這裡不再需要手動調用 UpdateStatsBasedOnRank()，基類會自動處理
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (Owner?.Creature == null) return;

        // 1. 執行格擋動作
        // 直接傳入 DynamicVars.Block，STS2 會自動處理敏捷 (Dexterity) 加成
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 執行攻擊動作
        // 使用 PreviewValue 確保吃得到力量與易傷加成
        await DamageCmd.Attack(DynamicVars.Damage.PreviewValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // await EXArt(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升級時減少費用
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank();
    }
}