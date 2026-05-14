using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Powers;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Linq;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class DomainOfTears : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-DOMAIN_OF_TEARS";
    public override string PortraitPath => "Temporarily.png".CardImagePath();

    public DomainOfTears() : base(4, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        JJCustomVar("M", 1m); // 預設打出 1 張
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null) return;

        // 計算當前星技等級對應的 X 值
        int starLevel = GetCurrentStarSkillLevel(player);
        int xValue = 1;
        if (starLevel >= 4 && starLevel <= 5) xValue = 2;
        else if (starLevel >= 6) xValue = 3;

        // 更新牌面顯示的 {M}
        if (DynamicVars.ContainsKey("M"))
        {
            DynamicVars["M"].BaseValue = xValue;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加能力，初始層數設為 (int)DynamicVars["M"].BaseValue 以便 UI 顯示
        await PowerCmd.Apply<DomainOfTearsPower>(
            Owner.Creature, 
            (int)DynamicVars["M"].BaseValue, 
            Owner.Creature, 
            this
        );
    }

    // 內部工具：獲取星技等級
    private int GetCurrentStarSkillLevel(Player player)
    {
        foreach (var relic in player.Relics)
        {
            if (relic is JiangXiaoMod.Code.Relics.IInnerStarMap starMap)
                return 1; // 此處請對接實際等級邏輯
        }
        return 1;
    }
}