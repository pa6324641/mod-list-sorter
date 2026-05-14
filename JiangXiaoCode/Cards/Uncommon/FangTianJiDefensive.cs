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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class FangTianJiDefensive : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-FANG_TIAN_JI_DEFENSIVE";
    
    // 基礎數值定義
    private const decimal BaseDamage = 9m;
    private const decimal BaseBlock = 3m;
    private const decimal BaseWeak = 1m;
    private const string MVar = "M";

    public FangTianJiDefensive() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        // 初始化傷害、格擋與關鍵字
        JJDamage(BaseDamage, ValueProp.Move);
        JJBlock(BaseBlock, ValueProp.Move);
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModHALBERD);
        JJPowerTip<WeakPower>();
        
        // 使用自定義變數 M 來儲存虛弱層數
        JJCustomVar(MVar, BaseWeak);
    }

    /// <summary>
    /// 隨 戟 Rank 成長邏輯：
    /// 1. 傷害：基礎 12 + (Rank * 3)
    /// 2. 護甲：基礎 6 + (Rank * 3)
    /// 3. 虛弱層數：1-3級:1層 / 4-5級:2層 / 6-7級:3層
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int rankLevel)
    {
        int rank = JiangXiaoUtils.GetHalberdRank(player);
        // 更新傷害成長
        DynamicVars.Damage.BaseValue = BaseDamage + ((rank - 1) * 2);

        // 更新格擋成長
        DynamicVars.Block.BaseValue = BaseBlock + ((rank - 1) * 2);
        if(IsUpgraded)
        {
            DynamicVars.Damage.BaseValue += 3m;
            DynamicVars.Block.BaseValue += 3m;
        }

        // 虛弱層數成長邏輯
        decimal weakAmt = 1m;
        if (rank >= 4 && rank <= 5)
        {
            weakAmt = 2m;
        }
        else if (rank >= 6)
        {
            weakAmt = 3m;
        }
        
        DynamicVars[MVar].BaseValue = weakAmt;
    }

    /// <summary>
    /// 卡牌打出邏輯
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        // 1. 獲取當前計算後的數值
        int weakAmount = (int)DynamicVars[MVar].BaseValue;
        
        // 2. 對全體敵人造成傷害並施加虛弱
        var enemies = CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            // 執行傷害
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
            
            // 施加虛弱 (STS2 中通常為 WeakPower)
            // [STS2_API] 使用 PowerCmd.Apply 異步執行
            await PowerCmd.Apply<WeakPower>(
                enemy, 
                weakAmount, 
                Owner.Creature, 
                this
            );
        }

        // 3. 我方全體獲得護甲 (包含隊友與玩家自己)
        var allies = CombatState.Allies;
        foreach (var ally in allies)
        {
            await CreatureCmd.GainBlock(ally, DynamicVars.Block, cardPlay);
        }
    }
    protected override void OnUpgrade()
    {
        UpdateStatsBasedOnRank();
    }
}