using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 悲傷之淚：每回合開始時造成目標最大生命值 {Amount}% 的傷害。
/// [設計規範] 1層 Amount = 1% 最大生命值
/// </summary>
public class TearOfSorrowPower : JiangXiaoPowerModel
{
	public const string PowerId = "JIANGXIAOMOD-TEAR_OF_SORROW_POWER";

	// 使用 Counter 類型，讓 Amount 直接代表百分比數值
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerType Type => PowerType.Debuff;

	public TearOfSorrowPower() : base() { }

	public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		// 判定是否為擁有者的回合開始
		if (Owner == null || side != Owner.Side || Amount <= 0) 
		{
			await base.AfterSideTurnStart(side, combatState);
			return;
		}

		// 1層 = 1%，所以直接除以 100
		decimal damagePercent = Amount / 100m;
		
		// 計算傷害：最大生命值 * (層數/100)，確保最小傷害為 1
		int finalDamage = (int)Math.Max(Math.Floor(Owner.MaxHp * damagePercent), 1m);

		Flash(); // 能力圖標閃爍

		// 執行無視格擋的傷害
		await CreatureCmd.Damage(
			new ThrowingPlayerChoiceContext(), 
			Owner, 
			(decimal)finalDamage, 
			ValueProp.Unblockable | ValueProp.Unpowered, 
			null, 
			null
		);

		await base.AfterSideTurnStart(side, combatState);
	}
}
