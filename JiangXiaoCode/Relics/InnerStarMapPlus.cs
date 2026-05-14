using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Commands;
using Godot;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMapPlus : CustomRelicModel, IInnerStarMap
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 用於升級時傳遞數據的靜態緩存
    public static int _transferPointsBuffer = -1;

    private int _skillPoints = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            // [新增]：限制技能點最大值為 99999
            int clampedValue = Math.Min(value, 99999);
            
            if (_skillPoints == clampedValue) return;
            _skillPoints = clampedValue;

            // [關鍵修正]：升級版也必須通知其他遺物刷新，保持與基礎版同步
            if (base.IsMutable && Owner != null)
            {
                var player = Owner;
                // 通知品質遺物
                player.Relics.OfType<StarSkillQuality>().FirstOrDefault()?.RefreshDisplay();
                // 通知等級遺物
                player.Relics.OfType<StarPowerLevel>().FirstOrDefault()?.RefreshDisplay();
                // [新增] 通知技藝遺物刷新 (對齊基礎版邏輯)
                player.Relics.OfType<BasicArts>().FirstOrDefault()?.RefreshDisplay();
            }

            RefreshDynamicText(); 
        } 
    }

    private const string VarPoints = "points";
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    public override bool ShouldReceiveCombatHooks => true;

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public void RefreshDynamicText()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    /// <summary>
    /// 當獲得升級版遺物時，從緩存中恢復點數
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        // 檢查是否有從升級前傳遞過來的數據
        if (_transferPointsBuffer != -1)
        {
            // 透過 Setter 賦值，會自動觸發 Clamp(99999) 與 UI 刷新
            this.JiangXiaoMod_SkillPoints = _transferPointsBuffer;
            _transferPointsBuffer = -1; // 使用後清除緩存
        }
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        // 升級版獲得更高的點數
        int gain = 1250;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = 5000;
            else if (room.RoomType == RoomType.Elite) gain = 2500;
        }

        if (Owner?.Creature?.CombatState != null)
        {
            CombatManager.Instance.History.StarsModified(Owner.Creature.CombatState, gain, Owner);
        }

        JiangXiaoMod_SkillPoints += gain;
        Flash(); 
        return Task.CompletedTask;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        // 升級版在進入非戰鬥房間時也獲得更多點數
        int gain = 1250;
        if (room.RoomType == RoomType.Event || room.RoomType == RoomType.Shop || room.RoomType == RoomType.RestSite || room.RoomType == RoomType.Treasure)
        {
            JiangXiaoMod_SkillPoints += gain;
            Flash(); 
        }
        return Task.CompletedTask;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarPoints, (decimal)JiangXiaoMod_SkillPoints);
        }
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    // [新增]：偵測北斗九星能力的施加
    public override async Task BeforeCombatStart()
    {
        if (!Owner.HasPower<BeiDouNineStarsPower>())
        {
            GD.Print("江曉成功啟動了北斗九星！");
            await PowerCmd.Apply<BeiDouNineStarsPower>(Owner.Creature, 1, null, null);
            Flash();
        }

        await base.BeforeCombatStart();
    }
}