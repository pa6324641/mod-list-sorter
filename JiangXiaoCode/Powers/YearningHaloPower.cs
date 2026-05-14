using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Commands.Builders;

namespace JiangXiaoMod.Code.Powers;

public class YearningHaloPower : JiangXiaoPowerModel
{
    public const string PowerId = "YearningHaloPower";

    public override PowerType Type => PowerType.Buff;
    
    // 設定為 Counter，代表層數即為強度 (百分比)
    public override PowerStackType StackType => PowerStackType.Counter;

    public YearningHaloPower() : base()
    {
    }

    // [重要] 移除 CanonicalVars。 
    // STS2 的 PowerModel 預設就會將 Amount 拋給本地化系統，
    // 您只需要在 powers.json 中使用 {Amount} 即可。

    public override async Task AfterAttack(AttackCommand command)
    {
        // 判定攻擊者是擁有者，且確實造成了傷害
        if (command.Attacker == Owner && command.Results != null)
        {
            // 獲取實際造成的未被格擋傷害
            int totalDamage = command.Results.Sum(r => r.UnblockedDamage);
            
            if (totalDamage > 0)
            {
                // 直接使用 Amount 作為百分比數值 (例如層數為 15，則代表 15%)
                decimal healPercent = Amount / 100m;
                
                // 使用 Math.Ceiling 確保有傷害就有回血 (例如 6 點傷害回 1 點)
                int healAmount = (int)Math.Ceiling(totalDamage * healPercent);

                if (healAmount > 0)
                {
                    // 執行治療
                    await CreatureCmd.Heal(Owner, (uint)healAmount);
                }
            }
        }
        await base.AfterAttack(command);
    }
}