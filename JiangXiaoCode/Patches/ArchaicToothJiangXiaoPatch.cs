using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JiangXiaoMod.Code.Cards.Ancient;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace JiangXiaoMod.Code.Patches;

/// <summary>
/// 處理古代牙齒在江曉角色下的預覽邏輯
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
public static class ArchaicToothJiangXiaoPatch
{
    // 江曉初始打擊的 ID
    private const string JiangXiaoStrikeId = "JIANGXIAOMOD-STRIKE_JIANG_XIAO";

    [HarmonyPrefix]
    public static bool SetupForPlayerPrefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        // 1. 安全檢查：確保 RunState 可用，避免在初始化未完成時訪問崩潰
        if (player.RunState == null) return true;

        // 2. 角色身份校驗：確保這套「神:技藝打擊」邏輯僅對江曉生效
        // [STS2_Logic] 透過玩家 ID 判斷當前冒險角色
        if (player.Character.Id.Entry != "JIANGXIAOMOD-JIANG_XIAO")
        {
            return true; // 非江曉角色，直接跳回原版邏輯處理普通的打擊/防禦轉換
        }

        // 3. 尋找牌組中是否有江曉的專屬初始卡
        // 使用 FirstOrDefault 配合 ID 檢索，確保精準匹配
        var starter = player.Deck.Cards.FirstOrDefault(c => c.Id.Entry == JiangXiaoStrikeId);

        // 如果在江曉的牌組中找不到對應的初始卡（例如被玩家刪除了），則回傳 true 讓原版邏輯嘗試尋找其他可用卡牌
        if (starter == null)
        {
            return true;
        }

        // 4. 準備預覽目標：神:技藝打擊
        // [STS2 API] 透過 RunState 提供的工廠方法創建卡牌實體，確保序列號與上下文正確
        var ancient = player.RunState.CreateCard<GodSkillStrike>(player);

        // 5. 處理升級繼承
        // 如果玩家目前的初始打擊已經強化過，生成的古代預覽卡也應保持一致
        if (starter.IsUpgraded)
        {
            CardCmd.Upgrade(ancient);
        }

        // 6. 設定遺物預覽數據
        // [API 遷移諮詢]：這在 STS1 是透過描述文本動態替換，在 STS2 中我們使用的是這種類似測試框架的數據綁定嗎？
        // 此處遵照使用者架構，將 Model 轉換為序列化格式進行 UI 綁定
        __instance.SetupForTests(starter.ToSerializable(), ancient.ToSerializable());

        // 7. 成功攔截並更新結果
        // 將 __result 設為 true，告知系統該遺物已完成初始化
        __result = true;
        
        // 返回 false 以阻止原版隨機尋找「打擊」或「防禦」標籤卡牌的邏輯
        return false;
    }
}