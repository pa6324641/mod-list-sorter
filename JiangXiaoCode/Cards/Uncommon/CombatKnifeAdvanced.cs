using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class CombatKnifeAdvanced : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_ADVANCED";
    private const string VarM = "M"; // 動態文本變數標籤
    private const string VarY = "Y"; // 動態文本變數標籤

    public CombatKnifeAdvanced() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);
        JJPowerTip<CombatKnifeAdvancedPower>();

        // 1. 初始化動態變數 M，預設為 1
        JJCustomVar(VarM, 1m);
        JJCustomVar(VarY, 6m);
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<CombatKnifeStable>(),
        HoverTipFactory.FromCard<CombatKnifeFlexible>()
    ];

    protected override void OnUpgrade()
    {
        // 2. 升級後變為 2 次選擇
        // DynamicVars[VarM].BaseValue = 2m;
        EnergyCost.UpgradeBy(-1);
        DynamicVars[VarY].UpgradeValueBy(3m);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null || Owner.PlayerCombatState == null) return;

        // 效果 1：賦予本回合觸發護甲的能力
        await PowerCmd.Apply<CombatKnifeAdvancedPower>(Owner.Creature, DynamicVars[VarY].BaseValue, Owner.Creature, this);

        // 效果 2：根據 M 的數值決定二選一的次數
        int selectionCount = (int)DynamicVars[VarM].BaseValue;
        for (int i = 0; i < selectionCount; i++)
        {
            var choices = new List<CardModel>
            {
                CombatState.CreateCard<CombatKnifeStable>(Owner),
                CombatState.CreateCard<CombatKnifeFlexible>(Owner)
            };

            if (IsUpgraded)
            {
                foreach (var token in choices)
                {
                    token.UpgradeInternal();
                }
            }

            var selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner);
            if (selectedCard != null)
            {
                selectedCard.AddKeyword(CardKeyword.Exhaust);
                await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, true);
                var copyForExhaust = selectedCard.CreateClone();
                // copyForExhaust.AddKeyword(CardKeyword.Exhaust);
                await CardPileCmd.Add(copyForExhaust, PileType.Exhaust);
            }
        }
    }
}