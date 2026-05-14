using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class BasicArts : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    // protected override string IconBaseName => "basic_arts";

    // --- 存檔字段定義 (使用簡化 Setter) ---

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int UnarmedPts { get; set; } = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int BladePts { get; set; } = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int BowPts { get; set; } = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int DaggerPts { get; set; } = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int HalberdPts { get; set; } = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int CombatKnifePts { get; set; } = 0;

    // --- 工具與配置 ---

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);
    public override bool ShouldReceiveCombatHooks => true;
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    /// <summary>
    /// 計算等級：10點一級，最高7級
    /// </summary>
    public int GetRank(int points) => Math.Min(7, (points / 10));

    /// <summary>
    /// 強制清空 STS2 的動態變量快取
    /// </summary>
    public void RefreshDisplay()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // [STS2_Optimization] 批量產出變量，對應 relics.json 中的標籤
            yield return new DynamicVar("UnarmedR", (decimal)GetRank(UnarmedPts));
            yield return new DynamicVar("UnarmedP", (decimal)UnarmedPts);
            yield return new DynamicVar("BladeR", (decimal)GetRank(BladePts));
            yield return new DynamicVar("BladeP", (decimal)BladePts);
            yield return new DynamicVar("BowR", (decimal)GetRank(BowPts));
            yield return new DynamicVar("BowP", (decimal)BowPts);
            yield return new DynamicVar("DaggerR", (decimal)GetRank(DaggerPts));
            yield return new DynamicVar("DaggerP", (decimal)DaggerPts);
            yield return new DynamicVar("HalberdR", (decimal)GetRank(HalberdPts));
            yield return new DynamicVar("HalberdP", (decimal)HalberdPts);
            yield return new DynamicVar("CombatKnifeR", (decimal)GetRank(CombatKnifePts));
            yield return new DynamicVar("CombatKnifeP", (decimal)CombatKnifePts);
        }
    }

    /// <summary>
    /// 卡片打出後的點數成長邏輯 (修正刷新頻率並限制最大值為 70)
    /// </summary>
    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // [STS2_Multiplayer_Safety] 核心判斷：
        // 檢查打出這張牌的 Owner 是否就是持有這個遺物的 Owner
        // 這樣可以防止隊友打牌、或是某些特殊召喚物打牌時，錯誤地增加你的技藝點數
        if (cardPlay.Card.Owner != this.Owner) 
        {
            return Task.CompletedTask;
        }

        var card = cardPlay.Card;
        bool hasGrown = false;

        // 檢測關鍵字並增加點數，且嚴格限制上限為 70
        if (card.IsJiangXiaoModUNARMED() && UnarmedPts < 70) { UnarmedPts++; hasGrown = true; }
        if (card.IsJiangXiaoModBLADE() && BladePts < 70) { BladePts++; hasGrown = true; }
        if (card.IsJiangXiaoModBOW() && BowPts < 70) { BowPts++; hasGrown = true; }
        if (card.IsJiangXiaoModDAGGER() && DaggerPts < 70) { DaggerPts++; hasGrown = true; }
        if (card.IsJiangXiaoModHALBERD() && HalberdPts < 70) { HalberdPts++; hasGrown = true; }
        if (card.IsJiangXiaoModCOMBATKNIFE() && CombatKnifePts < 70) { CombatKnifePts++; hasGrown = true; }

        if (hasGrown)
        {
            this.Flash();
            RefreshDisplay(); // 在這裡統一刷新一次即可
        }

        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    internal int GetArtRank(Player? player, BasicArtType type)
    {
        throw new NotImplementedException();
    }
}