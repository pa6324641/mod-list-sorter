using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace JiangXiaoMod.Monsters;

// [核心準則]：繼承 MonsterModel 並實作 ICustomModel 介面
public sealed class DecoyPet : MonsterModel, ICustomModel
{
    // 初始血量定義（這些值會作為 ModelDb 的基礎數值）
    public override int MinInitialHp => 10;
    public override int MaxInitialHp => 10;

    // 關鍵：必須設為 true 才會顯示血條 UI
    public override bool IsHealthBarVisible => true;

    // 指定視覺資源（.tscn 是 Godot 的場景文件）
    // 請確認你的檔案路徑與命名空間對齊
    protected override string VisualsPath => "res://JiangXiao/scenes/Pets/DecoyPet.tscn"; 

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 虛影分身通常不採取行動，返回一個閒置狀態
        MoveState idleState = new MoveState("IDLE", (Func<IReadOnlyList<Creature>, Task>)((IReadOnlyList<Creature> _) => Task.CompletedTask), Array.Empty<AbstractIntent>());
        idleState.FollowUpState = (MonsterState)(object)idleState;
        
        return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new List<MonsterState> { idleState }, (MonsterState)(object)idleState);
    }
}