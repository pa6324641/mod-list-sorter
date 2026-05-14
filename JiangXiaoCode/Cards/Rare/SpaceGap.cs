using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions; // 為了使用 JiangXiaoUtils
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class SpaceGap : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-SPACE_GAP";
    public const string Var = "M";
    public SpaceGap() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        // 初始化自定義變數 M (無實體層數)
        JJCustomVar(Var, 1m);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<IntangiblePower>();
        
    }



    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        // 升級基礎值 +1 (從 1 變成 2)
        DynamicVars["M"].UpgradeValueBy(1); 
    }

    /// <summary>
    /// [核心優化] 根據星技品質調整數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 取得當前的基礎值 (已考慮升級後的 2m 或 1m)
        // 注意：不直接 hardcode 避免與 OnUpgrade 衝突
        decimal baseAmount = IsUpgraded ? 2m : 1m;

        // 2. 根據星級計算最終數值
        // 當星級 >= 5 時，自身獲得的層數額外 +1
        if (skillRank >= 5)
        {
            DynamicVars["M"].BaseValue = baseAmount + 1m;
        }
        else
        {
            DynamicVars["M"].BaseValue = baseAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = this.CombatState;
        var player = this.Owner;
        if (combat == null || player?.Creature == null) return;

        // 1. 再次強制刷新數值（確保在星級變動的極端情況下數值正確）
        int currentRank = JiangXiaoUtils.GetSkillRank(player);
        ApplyRankLogic(player, currentRank);

        // 2. 獲取最終計算後的數值
        decimal intangibleAmount = DynamicVars["M"].BaseValue;
        
        // 3. 施加無實體給自己
        await PowerCmd.Apply<IntangiblePower>(player.Creature, intangibleAmount, player.Creature, this);

        // 4. 星級 5 以上的額外效果：隊友也獲得無實體
        if (currentRank >= 5)
        {
            // 尋找所有隊友 (排除自己)
            var allies = combat.Allies.Where(a => a != player.Creature);
            foreach (var ally in allies)
            {
                // 根據設計，隊友獲得 1 層 (可根據需求調整是否也要隨 M 縮放)
                await PowerCmd.Apply<IntangiblePower>(ally, 1m, player.Creature, this);
            }
        }
    }
}