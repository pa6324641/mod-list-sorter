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

namespace JiangXiaoMod.Code.Cards.Rare;

/// <summary>
/// 夏家刀法第三式-斜刀奪愛
/// 3費 攻擊 稀有
/// 效果：獲得格擋並造成傷害，數值隨 BladeRank 提升。
/// </summary>
[Pool(typeof(JiangXiaoCardPool))]
public sealed class XiaBladeStyle3 : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-XIA_BLADE_STYLE3";

    // 定義常量以便於維護
    private const decimal BaseVal = 3m;
    private const decimal RankBonus = 3m;
    private const decimal UpgradeBonus = 5m; // 升級後基礎值提升

    public XiaBladeStyle3() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBLADE);
        JJDamage(BaseVal, ValueProp.Move);
        JJBlock(BaseVal, ValueProp.Move);

        JJTag(CardTag.Strike);
    }

    /// <summary>
    /// 核心成長邏輯：由基類 JiangXiaoCardModel 自動在描述生成時或戰鬥中調用 [cite: 9]
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取當前刀法等級 
        int rank = JiangXiaoUtils.GetBladeRank(player);

        // 計算邏輯：(基礎值 + 升級值) + (等級 * 成長係數)
        // 這樣可以確保升級後的基礎提升與等級成長並存
        decimal currentBase = IsUpgraded ? (BaseVal + UpgradeBonus) : BaseVal;
        
        DynamicVars.Damage.BaseValue = currentBase + (rank * RankBonus);
        DynamicVars.Block.BaseValue = currentBase + (rank * RankBonus);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 基類會在 OnPlay 前自動調用 UpdateStatsBasedOnRank，此處可專注於動作執行
        
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (Owner?.Creature == null) return;

        // 1. 執行格擋動作 (目標為玩家自己)
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 執行攻擊動作
        // 注意：DamageCmd.Attack 通常接受 DynamicVar 以便計算 PreviewValue [cite: 3]
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);
        
        // await EXArt(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); 
    }
}