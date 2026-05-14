using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class CombatKnifeStable : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-COMBAT_KNIFE_STABLE";
    // 定義動態變數標籤
    private const string VarX = "X";
    private const string VarY = "Y";

    public CombatKnifeStable() : base(2, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModCOMBATKNIFE);
        JJKeywordAndTip(CardKeyword.Exhaust);

        // 註冊動態變數 X (攻擊牌數量/力量) 與 Y (技能牌數量/靈敏)
        JJCustomVar(VarX, 0m);
        JJCustomVar(VarY, 0m);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 確保在戰鬥中且動態變數已初始化
        if (player?.PlayerCombatState == null || DynamicVars == null) return;

        // 掃描消耗堆中具有「格鬥刀」關鍵字的卡牌
        var exhaustKnives = player.PlayerCombatState.ExhaustPile.Cards
            .Where(c => c.IsJiangXiaoModCOMBATKNIFE());

        // 分別計算攻擊牌 (X) 與技能牌 (Y) 的數量
        int xStrength = exhaustKnives.Count(c => c.Type == CardType.Attack);
        int yDexterity = exhaustKnives.Count(c => c.Type == CardType.Skill);

        // 更新動態文本顯示數值
        DynamicVars[VarX].BaseValue = xStrength;
        DynamicVars[VarY].BaseValue = yDexterity;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null) return;

        // 使用 ApplyRankLogic 中計算好的動態數值
        int xStrength = (int)DynamicVars[VarX].BaseValue;
        int yDexterity = (int)DynamicVars[VarY].BaseValue;

        // 依據掃描結果分別增加力量與靈敏
        if (xStrength > 0) await PowerCmd.Apply<StrengthPower>(Owner.Creature, xStrength, Owner.Creature, this);
        if (yDexterity > 0) await PowerCmd.Apply<DexterityPower>(Owner.Creature, yDexterity, Owner.Creature, this);
    }

        protected override void OnUpgrade()
    {
        DynamicVars[VarX].UpgradeValueBy(2m);
        DynamicVars[VarY].UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}