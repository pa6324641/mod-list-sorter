using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class RetrogradeLightEnemy : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-RETROGRADE_LIGHT_ENEMY";
    public override string PortraitPath => $"retrograde_light.png".CardImagePath();

    public RetrogradeLightEnemy() : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy, false)
    {
    }

    public override HashSet<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner?.Creature;
        var target = cardPlay.Target;

        if (player != null && target != null)
        {
            // [STS2_Logic] 計算雙方均值
            // 使用 HitPoints 屬性獲取當前血量 (大寫 P)[cite: 1]
            int averageHp = (player.CurrentHp + target.CurrentHp) / 2;

            // --- 處理玩家血量變動 ---
            if (player.CurrentHp < averageHp)
            {
                await CreatureCmd.Heal(player, averageHp - player.CurrentHp);
            }
            else if (player.CurrentHp > averageHp)
            {
                // [修正確認]：傳入 this 作為來源，使用 Unblockable 確保數值準確
                await CreatureCmd.Damage(choiceContext, player, player.CurrentHp - averageHp, ValueProp.Unblockable | ValueProp.Unpowered, this);
            }

            // --- 處理目標血量變動 ---
            if (target.CurrentHp < averageHp)
            {
                await CreatureCmd.Heal(target, averageHp - target.CurrentHp);
            }
            else if (target.CurrentHp > averageHp)
            {
                // [修正確認]：確保目標受到的傷害也來源於此卡
                await CreatureCmd.Damage(choiceContext, target, target.CurrentHp - averageHp, ValueProp.Unblockable | ValueProp.Unpowered, this);
            }
        }
    }
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        
    }
}