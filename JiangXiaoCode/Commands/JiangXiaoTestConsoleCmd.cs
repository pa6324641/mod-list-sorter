using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands; // 引用指令基類
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.DevConsole; // 引用您的遺物命名空間

namespace JiangXiaoMod.Code.DevConsole.ConsoleCommands;

/// <summary>
/// 江曉模組測試指令：jx_test
/// 功能：將玩家身上所有江曉遺物的點數設為滿值
/// </summary>
public class JiangXiaoTestConsoleCmd : AbstractConsoleCmd
{
    // 在控制台輸入的指令名稱
    public override string CmdName => "jx_test";

    // 此指令不需要參數
    public override string Args => "";

    public override string Description => "將江曉遺物的技能點與技藝等級設為滿值";

    // 設為 true 確保在多人模式中，室長輸入指令能同步給所有人
    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        // 1. 安全檢查：確保玩家在遊戲中
        if (issuingPlayer == null)
        {
            return new CmdResult(success: false, "此指令只能在進行中的遊戲內使用。");
        }

        // 2. 獲取該玩家身上的所有遺物
        // 根據範本，issuingPlayer 已經包含了我們需要的所有資訊
        var relics = issuingPlayer.Relics;
        bool foundRelic = false;

        foreach (var relic in relics)
        {
            // 處理星圖點數 (包含升級版)
            if (relic is IInnerStarMap starMap)
            {
                starMap.JiangXiaoMod_SkillPoints = 99999;
                relic.Flash();
                foundRelic = true;
            }

            // 處理基礎技藝等級
            if (relic is BasicArts arts)
            {
                arts.UnarmedPts = 70;
                arts.BladePts = 70;
                arts.BowPts = 70;
                arts.DaggerPts = 70;
                arts.HalberdPts = 70;
                arts.CombatKnifePts = 70;
                relic.Flash();
                foundRelic = true;
            }
        }

        // 3. 回傳執行結果
        if (foundRelic)
        {
            return new CmdResult(success: true, "江曉測試模式已啟動：技能點與技藝已刷滿！");
        }
        else
        {
            return new CmdResult(success: false, "當前角色身上找不到江曉的相關遺物。");
        }
    }
}