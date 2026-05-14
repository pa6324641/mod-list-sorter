using System.Threading.Tasks;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using Godot;
using JiangXiaoMod.Code.Cards.Rare;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers.Status;

public sealed class SilenceDamagePower : JiangXiaoPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter; 

    private bool _shouldStun = false;

    public SilenceDamagePower() : base() { }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource is SilenceOfVoid card) _shouldStun = card.TempStun;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || Owner.IsDead || side != Owner.Side) return;

        // 直接用 Amount 當作傷害數字
        if (this.Amount > 0)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, this.Amount, 
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }

        if (_shouldStun) await CreatureCmd.Stun(Owner);
    }
}