using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps; // 必須引用 ValueProp
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;

namespace JiangXiaoMod.Code.Cards.CardModels;

public abstract class JiangXiaoCardModel : CustomCardModel
{
    // [核心安全機制] 靜態鎖：防止 EXArt 觸發連鎖效應（例如格鬥刀打出另一張格鬥刀）導致死循環
    private static bool _isExecutingEXArt = false;
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    private readonly List<DynamicVar> _customVars = new();
    // private readonly List<IHoverTip> _customTips = new();
    private readonly List<System.Func<IHoverTip>> _customTipFactories = new();
    private readonly HashSet<CardKeyword> _customKeywords = new();
    private readonly HashSet<CardTag> _customTags = new();
    protected override IEnumerable<DynamicVar> CanonicalVars => _customVars;
    // protected override IEnumerable<IHoverTip> ExtraHoverTips => _customTips;
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var factory in _customTipFactories)
            {
                yield return factory();
            }
        }
    }
    public override HashSet<CardKeyword> CanonicalKeywords => _customKeywords;
    protected override HashSet<CardTag> CanonicalTags => _customTags;

    protected JiangXiaoCardModel(int cost, CardType type, CardRarity rarity, TargetType target, bool show = true)
        : base(cost, type, rarity, target, show) { }

    // --- 強化版變量添加方法 ---

    /// <summary>
    /// 最通用方法：直接添加任何 DynamicVar 實例 (包含 DamageVar, BlockVar)
    /// </summary>
    protected void JJVar(DynamicVar variable)
    {
        _customVars.Add(variable);
    }

    /// <summary>
    /// 專門添加傷害：自動封裝 DamageVar
    /// </summary>
    protected void JJDamage(decimal value, ValueProp prop = ValueProp.Move)
    {
        _customVars.Add(new DamageVar(value, prop));
    }

    /// <summary>
    /// 專門添加格擋：自動封裝 BlockVar
    /// </summary>
    protected void JJBlock(decimal value, ValueProp prop = ValueProp.Move)
    {
        _customVars.Add(new BlockVar(value, prop));
    }

    /// <summary>
    /// 添加自定義數值 (如 "M", "N")
    /// </summary>
    protected void JJCustomVar(string key, decimal value)
    {
        _customVars.Add(new DynamicVar(key, value));
    }

    // --- 其他工具方法 ---
    protected void JJTag(CardTag tag) => _customTags.Add(tag);
    // protected void JJKeywordAndTip(CardKeyword kw)
    // {
    //     _customKeywords.Add(kw);
    //     _customTips.Add(HoverTipFactory.FromKeyword(kw));
    // }
    protected void JJKeywordAndTip(CardKeyword kw)
    {
        _customKeywords.Add(kw);
        // 不要現在跑 FromKeyword，等到要顯示時再跑
        _customTipFactories.Add(() => HoverTipFactory.FromKeyword(kw));
    }
    // protected void JJStaticTip(StaticHoverTip tip) => _customTips.Add(HoverTipFactory.Static(tip));
    // protected void JJPowerTip<T>() where T : PowerModel
    // {
    //     _customTips.Add(HoverTipFactory.FromPower<T>());
    // }
    protected void JJStaticTip(StaticHoverTip tip) 
    {
        _customTipFactories.Add(() => HoverTipFactory.Static(tip));
    }

    protected void JJPowerTip<T>() where T : PowerModel
    {
        // 關鍵：將獲取邏輯封裝在 () => 中，這樣在 Card 創建時不會執行這行代碼
        _customTipFactories.Add(() => HoverTipFactory.FromPower<T>());
    }

    //複製自動刷新
    public void UpdateStatsBasedOnRank() 
    {
        // 獲取當前持有者
        var player = Owner;

        // 1. 抓取通用的「星技品質」等級 (預設為 1) [cite: 6, 7]
        int skillRank = JiangXiaoUtils.GetSkillRank(player);

        // 2. 執行應用邏輯，將 player 傳入以便子類抓取特定技藝等級
        ApplyRankLogic(player, skillRank);
    }
    // 定義一個抽象或虛擬函數，強迫或允許子類實作自己的計算公式
    protected abstract void ApplyRankLogic(Player? player, int skillRank);

    // 生成時自動觸發，動態文本
    public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
    {
        UpdateStatsBasedOnRank();
        return base.AfterCardGeneratedForCombat(card, addedByPlayer);
    }
    public override void AfterCreated()
    {
        UpdateStatsBasedOnRank();
        base.AfterCreated();
    }
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// STS2 鉤子：在第一回合抽牌前攔截，處理「被動星技」自動打出邏輯
    /// </summary>
    public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        // 修正判斷：既然 AddKeyword 是對 _keywords 操作，我們就直接檢查 _keywords
        // 或者為了保險起見，同時檢查自定義集合與基礎集合
        bool isPassive = _customKeywords.Contains(JiangXiaoModKeywords.Passive) || 
                        (this.Keywords != null && this.Keywords.Contains(JiangXiaoModKeywords.Passive));

        if (isPassive && 
            player?.PlayerCombatState != null && 
            combatState.RoundNumber == 1)
        {
            // 檢查當前卡牌是否在抽牌堆 (確保這張卡是從牌堆發動)
            if (player.PlayerCombatState.DrawPile.Cards.Contains(this))
            {
                // 更新數值（確保傷害/格擋與目前星技等級對齊）
                UpdateStatsBasedOnRank();

                // 執行自動打出指令
                await CardCmd.AutoPlay(choiceContext, this, player.Creature);
            }
        }

        // 呼叫基類實作
        if(player == null) return;
        await base.BeforeHandDrawLate(player, choiceContext, combatState);
    }

/// <summary>
    /// [STS2_JiangXiao] 基礎技藝額外獎勵 (EXArt)
    /// </summary>
    protected async Task EXArt(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // [遞迴保護]：如果正在執行連鎖，則不重複進入
        if (_isExecutingEXArt) return;

        var player = Owner;
        if (player?.PlayerCombatState == null) return;

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState?.Rng?.CombatTargets == null) return;
        var rng = runState.Rng.CombatTargets;

        _isExecutingEXArt = true; // 開啟鎖定

        try 
        {
            // 1. 徒手格鬥: 抽 1 張技能牌
            if (this.IsJiangXiaoModUNARMED() && JiangXiaoUtils.GetUnarmedRank(player) >= 4 && cardPlay.Card == this)
            {
                var skills = player.PlayerCombatState.DrawPile.Cards.Where(c => c.Type == CardType.Skill).ToList();
                if (skills.Any()) await CardPileCmd.Add(skills[rng.NextInt(skills.Count)], PileType.Hand);
            }

            // 2. 弓箭精通: 恢復 1 能量
            if (this.IsJiangXiaoModBOW() && JiangXiaoUtils.GetBowRank(player) >= 4 && cardPlay.Card == this)
            {
                await PlayerCmd.GainEnergy(1m, player);
            }

            // 3. 夏家刀法 (BLADE): 棄牌堆抽 1 張「非自身」的刀法牌
            if (this.IsJiangXiaoModBLADE() && JiangXiaoUtils.GetBladeRank(player) >= 4 && cardPlay.Card == this)
            {
                // 增加條件：c != this 確保不會抓到正在打出的這張牌
                var bladeCards = player.PlayerCombatState.DiscardPile.Cards
                    .Where(c => c.IsJiangXiaoModBLADE() && c.Id != this.Id)
                    .ToList();
                
                if (bladeCards.Any()) 
                {
                    await CardPileCmd.Add(bladeCards[rng.NextInt(bladeCards.Count)], PileType.Hand);
                }
            }

            // 4. 天方戟: 全體濺射 25% 傷害
            if (this.IsJiangXiaoModHALBERD() && JiangXiaoUtils.GetHalberdRank(player) >= 4 && cardPlay.Card == this)
            {
                decimal currentDmg = this.DynamicVars.Damage.EnchantedValue; 
                decimal splashDmg = Math.Max(1, (int)(currentDmg * 0.25m));
                var enemies = this.CombatState?.Enemies.ToList();
                if (enemies != null)
                {
                    foreach (var enemy in enemies) await CreatureCmd.Damage(choiceContext, enemy, splashDmg, ValueProp.Move, null, null);
                }
            }

            // 5. 格鬥刀: 25% 機率從「消耗堆」隨機打出，上限次數 = 1
            if (this.IsJiangXiaoModCOMBATKNIFE() && JiangXiaoUtils.GetCombatKnifeRank(player) >= 4 && cardPlay.Card == this)
            {
                // 執行 25% 機率判定
                if (rng.NextFloat() <= 0.25f)
                {
                    var validExhaustCards = player.PlayerCombatState.ExhaustPile.Cards
                        .Where(c => c != this && (c.Type == CardType.Attack || c.Type == CardType.Skill))
                        .ToList();

                    if (validExhaustCards.Any())
                    {
                        var targetCard = validExhaustCards[rng.NextInt(validExhaustCards.Count)];
                        // 自動打出選定的卡牌
                        for (int i = 0; i < 1 ; i++)
                        {
                            await CardCmd.AutoPlay(choiceContext, targetCard, ResolveTargetFor(targetCard, rng));    
                        }
                        
                    }
                }
            }

            // 6. 匕首: 25% 機率獲取 X 能量並複製
            if (this.IsJiangXiaoModDAGGER() && JiangXiaoUtils.GetDaggerRank(player) >= 4 && cardPlay.Card == this)
            {
                if (rng.NextFloat() <= 0.25f) 
                {
                    decimal energyToGain = DynamicVars.TryGetValue("X", out var xVar) && xVar.BaseValue != 0 ? xVar.BaseValue : 2m;
                    CardModel dupeCard = cardPlay.Card.CreateClone();
                    CardCmd.ApplyKeyword(dupeCard, CardKeyword.Exhaust);
                    await PlayerCmd.GainEnergy(energyToGain, player);
                    await CardCmd.AutoPlay(choiceContext, dupeCard, ResolveTargetFor(dupeCard, rng));
                }
            }
        }
        finally 
        {
            _isExecutingEXArt = false; // 解除鎖定
        }
    }

    // 輔助函式：優化隨機目標獲取
    protected Creature? ResolveTargetFor(CardModel card, Rng rng)
    {
        if (card.TargetType != TargetType.AnyEnemy || CombatState == null) return null;
        var enemies = CombatState.HittableEnemies.ToList();
        return enemies.Count == 0 ? null : enemies[rng.NextInt(enemies.Count)];
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayedLate(choiceContext, cardPlay);
        await EXArt(choiceContext, cardPlay);
    }

}