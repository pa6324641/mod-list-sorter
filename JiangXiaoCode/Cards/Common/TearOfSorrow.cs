using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class TearOfSorrow : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-TEAR_OF_SORROW";

    public TearOfSorrow() : base(3, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<TearOfSorrowPower>();
        
        // 初始化自定義變量 M 為 3 (對應 3%)
        JJCustomVar("M", 5m);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 依照您的需求：基礎調整為 3 層 (3%)
        decimal mAmount = skillRank switch
        {
            <= 3 => 5m, // Rank 1-3: 5%
            <= 5 => 10m, // Rank 4-5: 10%
            _    => 15m  // Rank 6+: 15%
        };

        // 升級後額外增加 5% (即 5層)
        if (IsUpgraded)
        {
            mAmount += 5m;
        }

        DynamicVars["M"].BaseValue = mAmount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null) return;

        // 確保數值與當前星技等級同步
        UpdateStatsBasedOnRank();
        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        int stacksToApply = (int)DynamicVars["M"].BaseValue;

        // 目標邏輯：Rank 1-3 影響全場；Rank 4+ 僅影響敵人
        IEnumerable<Creature> targets = (rank <= 3) 
            ? [.. CombatState.Allies, .. CombatState.Enemies] 
            : CombatState.Enemies;

        foreach (var target in targets)
        {
            // 這裡施加的 Amount 會直接變成能力中的層數
            await PowerCmd.Apply<TearOfSorrowPower>(target, (decimal)stacksToApply, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級減費並調高數值
        EnergyCost.UpgradeBy(-1); 
        UpdateStatsBasedOnRank();
    }
}