using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class Resentment : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD_RESENTMENT";
    
    // [STS2_Optimization] 建議使用 "M" 作為 Key，以便於 cards.json 調用 {M}
    private const string _mKey = "M";

    public Resentment() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // [STS2_Standard] 在構造函數註冊變量，會自動加入基類的 _customVars 列表
        JJCustomVar(_mKey, 30m);
        JJPowerTip<ResentmentPower>();
    }

    // [STS2_Logic] 此處負責處理隨星技等級變化的數值
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 使用 TryGetValue 確保變量安全存取
        if (DynamicVars.TryGetValue(_mKey, out var mVar))
        {
            // 邏輯：基礎 30 + (星技等級 * 10)
            // 等級 1 時機率為 40，等級 5 時為 80
            mVar.BaseValue = 30 + (skillRank * 10);
        }
    }

    public override Task BeforeCombatStart()
    {
        // 戰鬥開始前主動刷新一次數值
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出時再次刷新，確保機率與當前等級同步
        UpdateStatsBasedOnRank();

        if (Owner?.Creature != null)
        {
            // 施加能力。注意：ResentmentPower 內部若也需此機率數值，
            // 建議在 Power 類中同樣調用 JiangXiaoUtils.GetSkillRank(player)
            await PowerCmd.Apply<ResentmentPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級效果：能量消耗 1 -> 0
        EnergyCost.UpgradeBy(-1);
        AddKeyword(JiangXiaoModKeywords.Passive);
    }
}