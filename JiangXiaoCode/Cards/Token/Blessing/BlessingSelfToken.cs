using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
namespace JiangXiaoMod.Code.Cards.Token;
using MegaCrit.Sts2.Core.Entities.Players;

[Pool(typeof(TokenCardPool))]
public sealed class BlessingSelfToken : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-BLESSING_TOKEN_SELF";
    public BlessingSelfToken() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }
    public override string PortraitPath => $"blessing.png".CardImagePath();
    
    public override HashSet<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HealAmount", 6m)];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars["HealAmount"].BaseValue);
    }
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        
    }
}