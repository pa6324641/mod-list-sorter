using System.Linq;
using System.Threading.Tasks;
using Godot;
using JiangXiaoMod.Monsters;
using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace JiangXiaoMod.Code.Commands;

public static class DecoyCmd
{
    // 召喚誘餌的非同步指令
    public static async Task<Creature?> Summon(Player summoner, decimal hp, int maxCount = 3)
    {
        if (summoner?.Creature?.CombatState == null || summoner.PlayerCombatState == null)
        {
            return null;
        }

        int currentDecoyCount = GetDecoyCount(summoner);
        if (currentDecoyCount >= maxCount)
        {
            return null; 
        }

        Creature decoy = await PlayerCmd.AddPet<DecoyPet>(summoner);

        await CreatureCmd.SetMaxHp(decoy, hp);
        await CreatureCmd.Heal(decoy, hp, false);

        EnsureDecoyNodeSettings(summoner, decoy);

        // 觸發星圖能力繼承邏輯
        await InheritStarMapPowers(summoner, decoy);

        return decoy;
    }

    // 邏輯分支：尋找並讓誘餌繼承召喚者身上的星圖能力
    private static async Task InheritStarMapPowers(Player summoner, Creature decoy)
    {
        if (summoner.Creature == null || decoy == null) return;

        // 取得召喚者身上所有繼承自 StarMapPowerModel 的星圖能力
        var starMapPowers = summoner.Creature.GetPowerInstances<StarMapPowerModel>();

        foreach (var power in starMapPowers)
        {
            Type powerType = power.GetType();

            // 透過反射找出官方 API，增加對第一個參數型別的檢查
            var applyMethod = typeof(PowerCmd).GetMethods()
                .FirstOrDefault(m => m.Name == "Apply" && 
                                     m.IsGenericMethod && 
                                     m.GetParameters().Length == 5 &&
                                     m.GetParameters()[0].ParameterType == typeof(Creature)); // 確保抓到的是單體目標的方法

            if (applyMethod != null)
            {
                var genericApply = applyMethod.MakeGenericMethod(powerType);

                // 呼叫單體賦予能力的方法
                var task = genericApply.Invoke(null, new object?[] { decoy, (decimal)power.Amount, summoner.Creature, null, false }) as Task;
                
                if (task != null)
                {
                    await task;
                }
            }
        }
    }

    // 邏輯分支：計算目前場上存活的誘餌數量
    private static int GetDecoyCount(Player summoner)
    {
        if (summoner.PlayerCombatState?.Pets == null) return 0;
        return summoner.PlayerCombatState.Pets.Count(p => p.Monster is DecoyPet && p.IsAlive);
    }

    // 邏輯分支：確保 Godot 視覺節點的互動性
    private static void EnsureDecoyNodeSettings(Player summoner, Creature decoy)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null) return;

        NCreature? creatureNode = room.GetCreatureNode(decoy);
        
        if (creatureNode != null)
        {
            creatureNode.ToggleIsInteractable(true);
        }
    }
}