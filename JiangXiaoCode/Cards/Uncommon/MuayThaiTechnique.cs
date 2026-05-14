using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class MuayThaiTechnique : JiangXiaoCardModel
{
    private const decimal BaseDmg = 5m;
    private const decimal DmgBonusPerRank = 1m;
    private const string VarM = "M";

    public MuayThaiTechnique() : base(
        cost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        // 添加徒手標籤與提示
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModUNARMED);

        // 初始化傷害值
        JJDamage(BaseDmg, ValueProp.Move);
        
        // 初始化自定義變數 M (力量)，初始為 1
        JJCustomVar(VarM, 1m);
    }

    /// <summary>
    /// 核心修正：根據當前等級「重新設定」而非「累加」數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int rank)
    {
        // 1. 更新傷害：基礎 5 + (等級 * 1)
        // 這裡直接賦值，確保每次計算都是從 5 開始
        int unarmedRank = JiangXiaoUtils.GetUnarmedRank(player);
        DynamicVars.Damage.BaseValue = BaseDmg + (unarmedRank * DmgBonusPerRank);

        // 2. 更新力量 (M)：基礎 1 (若升級則為 2) + Floor((rank-1) / 3)
        // 判定升級狀態來決定起始 M 值
        decimal startingM = IsUpgraded ? 2m : 1m;
        
        // 計算等級帶來的加成 (每 3 級 +1)
        int bonusM = (int)Math.Floor((unarmedRank - 1) / 3m);

        // 直接賦值給 BaseValue，解決數值膨脹問題
        DynamicVars[VarM].BaseValue = startingM + bonusM;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 確保打出前數值最新
        UpdateStatsBasedOnRank();

        // 1. 執行攻擊動作 (使用 FinalValue 以包含力量等加成，或 BaseValue 僅抓取卡面數值)
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 賦予自身力量
        int strengthAmount = (int)DynamicVars[VarM].BaseValue;
        if (strengthAmount > 0)
        {
            // [STS2_API] 使用 PowerCmd 賦予能力
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, strengthAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級時將 M 的基礎數值永久提升 1 (這會反映在 ApplyRankLogic 的 IsUpgraded 判定中)
        // 由於 ApplyRankLogic 會重新計算 BaseValue，這裡主要是觸發更新
        UpdateStatsBasedOnRank();
    }
}