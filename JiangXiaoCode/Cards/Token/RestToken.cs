using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class RestToken : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-REST_TOKEN";
    private const string HealVar = "HealAmount"; // 定義變量標籤

    public RestToken() : base(2, CardType.Skill, CardRarity.Token, TargetType.None)
    {
        // [STS2_API] 初始化自定義變量，初始值設為 0
        // 這會讓卡牌具備追蹤名為 "HealAmount" 數值的能力 
        JJCustomVar(HealVar, 0);
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// 動態數值計算邏輯
    /// 每當卡牌數值需要重繪（如進入手牌、屬性變更）時會觸發此方法
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 安全檢查：若戰鬥尚未開始或玩家實體不存在，則不計算
        if (player?.Creature == null) return;

        // 2. 計算治療量 (最大生命值的 40%)
        // 使用 decimal 進行精確計算後，以 Math.Ceiling 進位
        decimal calculatedHeal = Math.Ceiling(player.Creature.MaxHp * 0.4m);

        // 3. 更新動態變量的基礎值，這會直接反映在卡牌描述的 {HealAmount} 位置 
        DynamicVars[HealVar].BaseValue = calculatedHeal;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 直接從計算好的變量中取得數值
        int healAmount = (int)DynamicVars[HealVar].BaseValue;

        // 執行治療
        // 參數：目標, 數值, 是否顯示綠色治療數字特效
        await CreatureCmd.Heal(Owner.Creature, healAmount, true);

        // 結束回合
        // [STS2_API] 呼叫 PlayerCmd 進行回合結束處置
        PlayerCmd.EndTurn(Owner, false);
    }
}