using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Commands;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

// namespace JiangXiaoMod.Code.Cards.Rare;
namespace JiangXiaoMod.Code.Cards.Token;


[Pool(typeof(JiangXiaoCardPool))]
public sealed class Decoy : JiangXiaoCardModel
{
    private const int BaseHp = 10;
    private const int RankHpBonus = 5;
    private const string Value = "M";
    public override string PortraitPath => "Temporarily.png".CardImagePath();

    // public Decoy() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    public Decoy() : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        JJKeywordAndTip(CardKeyword.Exhaust);
        JJCustomVar(Value, BaseHp);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        DynamicVars[Value].BaseValue = BaseHp + (skillRank * RankHpBonus);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // [STS2_Logic] 說明：召喚實體牽涉到動畫與遊戲內核更新，必須使用 await 等待任務完成
        await DecoyCmd.Summon(Owner, DynamicVars[Value].BaseValue);
    }
}