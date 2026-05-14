using BaseLib.Abstracts;
using Godot;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Random; 
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq; // 必須新增，用於 FirstOrDefault
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 怨氣能力：受傷時機率對敵人施加隨機負面效果。
/// 已修正 DynamicVar 渲染邏輯，確保在 UI 預覽時也能正確顯示數值。
/// </summary>
public class ResentmentPower : JiangXiaoPowerModel
{
    public const string PowerId = "RESENTMENT_POWER";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    private const string VarM = "M"; 

    public ResentmentPower() : base()
    {
    }

    // [STS2_Optimization] 建立一個健壯的等級獲取邏輯
    private int GetCurrentSkillRank()
    {
        // 優先從擁有者獲取，若在 UI 預覽/圖鑑中，則從全域 RunManager 獲取
        var player = Owner?.Player ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        return JiangXiaoUtils.GetSkillRank(player);
    }

    private int CalculateTriggerChance()
    {
        int rank = GetCurrentSkillRank();
        return 30 + (rank * 10);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算好的數值以 decimal 形式交給 SmartFormat 系統渲染
            yield return new DynamicVar(VarM, (decimal)CalculateTriggerChance());
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 1. 安全檢查
        if (target != Owner || dealer == null || dealer == target)
        {
            return;
        }

        // 2. 獲取全域狀態以存取同步 RNG
        var runState = RunManager.Instance.DebugOnlyGetState();
        
        // 3. 確保 RNG 序列存在
        if (runState?.Rng?.CombatTargets == null)
        {
            return;
        }

        Rng rng = runState.Rng.CombatTargets;

        // 4. 判定是否觸發
        float chance = CalculateTriggerChance() / 100f;
        if (rng.NextFloat() <= chance)
        {
            await ApplyRandomDebuff(choiceContext, dealer, rng);
        }

        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
    }

    private async Task ApplyRandomDebuff(PlayerChoiceContext context, Creature dealer, Rng rng)
    {
        int currentRank = GetCurrentSkillRank();
        int varAmount = currentRank * 2;

        int roll = rng.NextInt(0, 7); 

        Flash();

        switch (roll)
        {
            case 0: await PowerCmd.Apply<DexterityPower>(dealer, -varAmount, Owner, null); break; // 修正：負面效果應為負數
            case 1: await PowerCmd.Apply<VulnerablePower>(dealer, varAmount, Owner, null); break;
            case 2: await PowerCmd.Apply<FrailPower>(dealer, varAmount, Owner, null); break;
            case 3: await PowerCmd.Apply<ShrinkPower>(dealer, varAmount, Owner, null); break;
            case 4: await PowerCmd.Apply<PoisonPower>(dealer, varAmount * 3, Owner, null); break;
            case 5: await PowerCmd.Apply<DoomPower>(dealer, varAmount * 3, Owner, null); break;
            case 6: await PowerCmd.Apply<StrengthPower>(dealer, -varAmount, Owner, null); break;
            case 7: 
            default:
                await PowerCmd.Apply<WeakPower>(dealer, varAmount, Owner, null); 
                break;
        }
    }
}