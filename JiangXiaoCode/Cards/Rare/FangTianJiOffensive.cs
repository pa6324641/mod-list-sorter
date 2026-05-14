using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Rare;

/// <summary>
/// 方天戟：攻勢
/// 3費 攻擊 罕見
/// 效果：對全體敵人造成傷害，傷害與攻擊次數隨「戟 Rank」提升。
/// </summary>
[Pool(typeof(JiangXiaoCardPool))]
public sealed class FangTianJiOffensive : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-FANG_TIAN_JI_OFFENSIVE";
    
    // 基礎數值定義
    private const decimal BaseDamage = 9m;
    private const decimal DamagePerRank = 2m;
    private const string XVar = "X"; // 用於動態顯示攻擊次數的變量

    public FangTianJiOffensive() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        // 1. 初始化基礎傷害（使用 ValueProp.Move 確保數值動態渲染）
        JJDamage(BaseDamage, ValueProp.Move);
        
        // 2. 初始化「戟」關鍵字與提示
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModHALBERD);
        
        // 3. 初始化自定義變數 X，用於在描述中顯示攻擊次數（例如：造成 {Damage} 傷害 {X} 次）
        JJCustomVar(XVar, 1m);
    }

    /// <summary>
    /// 隨 戟 Rank 成長邏輯 ：
    /// 1. 傷害：基礎 12 + (HalberdRank * 3)
    /// 2. 攻擊次數：
    ///    - 1-3 Rank: 1次
    ///    - 4-5 Rank: 2次
    ///    - 6-7 Rank: 3次
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取當前「戟」的等級 
        int rank = JiangXiaoUtils.GetHalberdRank(player);
        
        // 更新傷害基礎值：12 + (Rank * 2)
        DynamicVars.Damage.BaseValue = BaseDamage + ((rank - 1) * DamagePerRank);
        if(IsUpgraded)
        {
            DynamicVars.Damage.BaseValue += 3m;
        }

        // 更新攻擊次數邏輯
        decimal hitCount = 1m;
        if (rank >= 4 && rank <= 5)
        {
            hitCount = 2m;
        }
        else if (rank >= 6)
        {
            hitCount = 3m;
        }
        
        // 將計算出的次數寫回動態變量 X，供本地化文本調用
        DynamicVars[XVar].BaseValue = hitCount;
    }

    /// <summary>
    /// 卡牌打出邏輯
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        // 1. 獲取當前計算後的數值
        int totalHits = (int)DynamicVars[XVar].BaseValue;
        var damageValue = DynamicVars.Damage.BaseValue;

        // 2. 執行多段全體傷害
        // 依據 totalHits 進行循環攻擊
        for (int i = 0; i < totalHits; i++)
        {
            // 獲取所有可被攻擊的敵人
            var enemies = CombatState.HittableEnemies;
            foreach (var enemy in enemies)
            {
                // [STS2_API] 執行傷害命令，並指定當前循環中的敵人為目標 
                await DamageCmd.Attack(damageValue)
                    .FromCard(this)
                    .Targeting(enemy)
                    .Execute(choiceContext);
            }
        }
        DynamicVars.Damage.BaseValue *= totalHits ;
    }
    protected override void OnUpgrade()
    {
        UpdateStatsBasedOnRank();
    }
}