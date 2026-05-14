using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;
using JiangXiaoMod.Code.Powers.Status;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class SilenceOfVoid : JiangXiaoCardModel
{
    public const string CardId = "SilenceOfVoid";
    private const string TurnVar = "T";
    private const string DamageVarKey = "DPercent";

    // 改為公開屬性，讓 Power 可以讀取
    public decimal TempDmgPct { get; private set; }
    public bool TempStun { get; private set; }
    private int _tempDuration;
    public override string PortraitPath => "Temporarily.png".CardImagePath();

    public SilenceOfVoid() : base(5, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        JJCustomVar(TurnVar, 1);
        JJCustomVar(DamageVarKey, 0);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<SilencePower>();
        JJPowerTip<SilenceDamagePower>();
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        int duration = 1;
        decimal dmgPct = 0m;
        bool stun = false;

        if (skillRank <= 2) { duration = 1; dmgPct = 0m; }
        else if (skillRank <= 4) { duration = 2; dmgPct = 0.10m; }
        else if (skillRank <= 6) { duration = 2; dmgPct = 0.20m; stun = true; }
        else { duration = 3; dmgPct = 0.30m; stun = true; }

        DynamicVars[TurnVar].BaseValue = duration;
        DynamicVars[DamageVarKey].BaseValue = dmgPct * 100;
        
        _tempDuration = duration;
        TempDmgPct = dmgPct; 
        TempStun = stun;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        foreach (var enemy in CombatState.HittableEnemies)
        {
            // 先掛主能力
            await PowerCmd.Apply<SilencePower>(new[] { enemy }, (decimal)_tempDuration, Owner.Creature, this);
            
            // 再計算並掛傷害
            int actualDamage = (int)(enemy.MaxHp * TempDmgPct);
            if (actualDamage > 0 || TempStun)
            {
                await PowerCmd.Apply<SilenceDamagePower>(new[] { enemy }, (decimal)actualDamage, Owner.Creature, this);
            }
        }
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank();
    }
}