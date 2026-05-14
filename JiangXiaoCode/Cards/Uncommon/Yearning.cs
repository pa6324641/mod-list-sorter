using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using JiangXiaoMod.Code.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))] 
public class Yearning : JiangXiaoCardModel
{
    private const string VarM = "M";

    public Yearning() : base(
        3, 
        type: CardType.Power, 
        rarity: CardRarity.Uncommon, 
        target: TargetType.Self
    )
    {
        // 初始化自定義變量 M 用於卡面顯示
        JJCustomVar(VarM, 10m);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<YearningHaloPower>();
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 依照您的註解公式：5 + (等級 * 5) 
        // 等級 1: 10%
        // 等級 2: 15%
        // 等級 3: 20%
        DynamicVars[VarM].BaseValue = 5m + (skillRank * 5m);
    }

    protected override void OnUpgrade()
    {
        // 升級降低費用的邏輯不變
        EnergyCost.UpgradeBy(-2);
        UpdateStatsBasedOnRank();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null || Owner?.Creature == null) return;

        // 確保數值是最新的
        UpdateStatsBasedOnRank();
        int lifestealPercent = (int)DynamicVars[VarM].BaseValue;

        // 尋找擁有「DawnPower」能力的所有盟友
        var alliesWithDawn = combat.Allies
            .Where(a => a.Powers.Any(p => p is DawnPower))
            .ToList();

        if (alliesWithDawn.Any())
        {
            // 將計算出的百分比作為「層數」施加
            await PowerCmd.Apply<YearningHaloPower>(alliesWithDawn, lifestealPercent, Owner.Creature, this);
        }
        else
        {
            // 對全場單位施加
            var allUnits = combat.Allies.Concat(combat.Enemies).ToList();
            await PowerCmd.Apply<YearningHaloPower>(allUnits, lifestealPercent, Owner.Creature, this);
        }
    }
}