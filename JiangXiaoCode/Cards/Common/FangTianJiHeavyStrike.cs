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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class FangTianJiHeavyStrike : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-FANG_TIAN_JI_HEAVY_STRIKE";
    
    // 基礎數值定義
    private const decimal BaseDamage = 9m;
    private const decimal BaseVulnerable = 1m;
    private const string MVar = "M";

    public FangTianJiHeavyStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        JJDamage(BaseDamage, ValueProp.Move);
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModHALBERD);
        JJPowerTip<VulnerablePower>();
        JJCustomVar(MVar, BaseVulnerable);
    }

    /// <summary>
    /// 隨 戟 Rank 成長邏輯：
    /// 1. 每 rank + 2 點傷害
    /// 2. 1-3 rank: 1層易傷 / 4-5 rank: 2層 / 6-7 rank: 3層
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int rankLevel)
    {
        int rank = JiangXiaoUtils.GetHalberdRank(player);
        // 傷害成長：基礎 9 + ((rank - 1) * 2)
        DynamicVars.Damage.BaseValue = BaseDamage + ((rank - 1) * 2);
        if(IsUpgraded)
        {
            DynamicVars.Damage.BaseValue += 3m;
        }

        // 易傷層數成長
        decimal vulnerAmt = 1m;
        if (rank >= 4 && rank <= 5)
        {
            vulnerAmt = 2m;
        }
        else if (rank >= 6)
        {
            vulnerAmt = 3m;
        }
        
        DynamicVars[MVar].BaseValue = vulnerAmt;
    }

    /// <summary>
    /// 卡牌打出邏輯：對全體敵人造成傷害並施加易傷
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保戰鬥狀態存在
        if (CombatState == null) return;

        // 獲取當前計算後的易傷層數
        int vulnerVar = (int)DynamicVars[MVar].BaseValue;
        
        // 獲取目前戰鬥中所有可被攻擊的敵人
        var targets = CombatState.HittableEnemies;

        // 執行全體傷害指令 (AOE 視覺處理)


        // 為每個敵人施加易傷能力
        foreach (var enemy in targets)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(enemy)
            .Execute(choiceContext);

            await PowerCmd.Apply<VulnerablePower>(
                enemy, 
                vulnerVar, 
                Owner.Creature, 
                this
            );
        }
        
    }
    protected override void OnUpgrade()
    {
        UpdateStatsBasedOnRank();
    }
}