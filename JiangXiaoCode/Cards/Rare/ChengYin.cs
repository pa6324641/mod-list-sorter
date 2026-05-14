using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace JiangXiaoMod.Code.Cards.Rare
{
    [Pool(typeof(JiangXiaoCardPool))]
    public sealed class ChengYin : JiangXiaoCardModel
    {
        public const string CardId = "JIANGXIAOMOD-CHENG_YIN";
        private const string mKey = "M";

        public ChengYin() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
            // [STS2_Optimization] JJKeywordAndTip 會自動處理關鍵字與懸停提示
            JJKeywordAndTip(JiangXiaoModKeywords.Star);
            
            // 註冊能力提示
            JJPowerTip<ChengYinPower>();
            JJPowerTip<RegenPower>();
            
            // 初始化自定義變量 M
            JJCustomVar(mKey, 2m); 
        }

        /// <summary>
        /// 根據星技品質等級動態調整數值。
        /// [STS2_API] 當遺物狀態改變或進入戰鬥時，基類會觸發此邏輯。
        /// </summary>
        protected override void ApplyRankLogic(Player? player, int skillRank)
        {
            // 基礎 2 層，每提升一級品質 +1。
            // 例如：1級時為2層，2級時為3層。
            decimal finalValue = 2m + (skillRank - 1);
            
            // 更新 DynamicVar，這會直接影響卡牌描述中的 {M} 顯示
            if (DynamicVars.ContainsKey(mKey))
            {
                DynamicVars[mKey].BaseValue = finalValue;
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 確保在出牌瞬間數值是根據當前狀態計算的
            UpdateStatsBasedOnRank();
            decimal amount = DynamicVars[mKey].BaseValue;

            var combat = this.CombatState;
            if (combat == null) return;

            // 1. 執行卡牌播放視覺效果 (如有需要可加入 VFX 指令)

            // 2. [STS2_API] 批量施加專屬能力給所有盟友 (包含玩家)
            // PowerCmd.Apply 會自動處理 Task 佇列，確保動畫依序播放
            await PowerCmd.Apply<ChengYinPower>(
                combat.Allies, 
                amount, 
                Owner.Creature, 
                this
            );

            // 3. 同時施加標準的「再生」能力
            await PowerCmd.Apply<RegenPower>(
                combat.Allies, 
                amount, 
                Owner.Creature, 
                this
            );
        }
        protected override void OnUpgrade()
        {
            DynamicVars[mKey].UpgradeValueBy(2m);
        }
    }
}