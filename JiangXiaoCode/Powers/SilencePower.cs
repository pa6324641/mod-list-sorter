using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers.Status;

public sealed class SilencePower : JiangXiaoPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter; 
    
    // [關鍵修復 1] 移除 readonly，因為我們要在套用時重新賦值
    private List<(Type PowerType, decimal Amount)> _storedBuffs = new();

    public SilencePower() : base() { }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // ==========================================
        // [關鍵修復 2] 斬斷淺拷貝 (Shallow Copy) 的連結
        // 在能力正式掛到怪物身上時，強制建立一個全新的清單。
        // 這樣每隻怪物就會擁有完全獨立的記憶體空間，再也不會搶 Buff！
        // ==========================================
        _storedBuffs = new List<(Type PowerType, decimal Amount)>();

        if (Owner != null)
        {
            // 只抓取「目前這個 Owner」身上的 Buff
            var buffs = Owner.Powers.Where(p => p.Type == PowerType.Buff && p != this).ToList();
            foreach (var b in buffs)
            {
                _storedBuffs.Add((b.GetType(), b.Amount));
                await PowerCmd.Remove(b);
            }
            GD.Print($"[SilencePower] {Owner.ToString()} 獨立封存了 {_storedBuffs.Count} 個 Buff");
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || Owner.IsDead || side != Owner.Side) return;
        // 減少層數
        await PowerCmd.Apply<SilencePower>(new[] { Owner }, -1, null, null);
    }

    public override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        // 確保是「這個實例」且層數歸零
        if (power == this && (this.Amount + amount <= 0))
        {
            await RestoreBuffs();
            
            // 同步移除傷害能力
            var dmgPower = Owner?.Powers.FirstOrDefault(p => p is SilenceDamagePower);
            if (dmgPower != null) await PowerCmd.Remove(dmgPower);
        }
    }

    private async Task RestoreBuffs()
    {
        // 嚴格檢查
        if (Owner == null || Owner.IsDead || _storedBuffs.Count == 0) 
        {
            _storedBuffs?.Clear();
            return;
        }

        string ownerName = Owner.ToString();
        GD.Print($"[SilencePower] 正在為 {ownerName} 還原 {_storedBuffs.Count} 個 Buff...");

        var methods = typeof(PowerCmd).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var applyMethod = methods.FirstOrDefault(m => 
            m.Name == "Apply" && m.IsGenericMethod && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(IEnumerable<Creature>));

        if (applyMethod != null)
        {
            int paramCount = applyMethod.GetParameters().Length;
            // 鎖定目標陣列
            var targetArray = new[] { Owner };

            foreach (var buff in _storedBuffs)
            {
                try 
                {
                    var generic = applyMethod.MakeGenericMethod(buff.PowerType);
                    object?[] parameters = new object?[paramCount];
                    parameters[0] = targetArray; 
                    parameters[1] = buff.Amount; 
                    if (paramCount > 2) parameters[2] = null;  
                    if (paramCount > 3) parameters[3] = null;  
                    if (paramCount > 4) parameters[4] = false; 

                    var result = generic.Invoke(null, parameters);
                    if (result is Task task) await task;
                    
                    GD.Print($"[SilencePower] 已還原 {buff.PowerType.Name} 給 {ownerName}");
                } 
                catch (Exception e) { GD.PrintErr($"[SilencePower] 還原失敗: {e.Message}"); }
            }
        }
        
        _storedBuffs?.Clear();
    }

    // --- 死亡與移除清理機制 ---
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        _storedBuffs?.Clear();
        return base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }
}