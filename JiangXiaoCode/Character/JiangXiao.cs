using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using JiangXiaoMod.Code.Cards.Basic; 
using JiangXiaoMod.Code.Relics;
using System.Linq;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using JiangXiaoMod.Code.Nodes;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.Rare;
using JiangXiaoMod.Code.Cards.Common;
using JiangXiaoMod.Code.Cards.Uncommon;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using BaseLib.Patches.UI;
using MegaCrit.Sts2.addons.mega_text;
using JiangXiaoMod.Code.Cards.Ancient;


namespace JiangXiaoMod.Code.Character;

// public class JiangXiao : PlaceholderCharacterModel
public class JiangXiao : CustomCharacterModel
{
	public const string CharacterId = "JiangXiao";
	// 角色代表顏色：極深紅
	public static readonly Color Color = new(0.2f, 0.0f, 0.0f);
	public override Color MapDrawingColor => Color;
	public override Color NameColor => Color;
	public override CharacterGender Gender => CharacterGender.Masculine;
	protected override CharacterModel? UnlocksAfterRunAs => null;
	public override int StartingHp => 75; 
	public override int StartingGold => 99;
	
	// --- 能量球配置 [修復版：純貼圖模式] ---
	// 移除 CustomIconPath 以免觸發 %BurstBack 崩潰

	 public override string CustomTrailPath => "res://JiangXiao/scenes/JiangXiao/card_trail_jiangxiao.tscn";

	// public override CustomEnergyCounter? CustomEnergyCounter => 
	//     new CustomEnergyCounter(EnergyCounterPaths, new Color(0.545f, 0f, 0f), new Color(0.7f, 0.1f, 0.9f));
	// private string EnergyCounterPaths(int i)
	// {
	//     // 確保 i 對應你資料夾中的 0, 1, 2, 3, 4, 5 層級圖片
	//     return $"res://JiangXiao/images/ui/combat/energy_counters/jiangxiao/jiangxiao_orb_layer_{i}.png";
	// }

	public override string CustomEnergyCounterPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_energy_counter.tscn";

	// 初始卡組
	public override IEnumerable<CardModel> StartingDeck => [
		ModelDb.Card<StrikeJiangXiao>(), 
		ModelDb.Card<StrikeJiangXiao>(), 
		ModelDb.Card<StrikeJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<Blessing>(),
		ModelDb.Card<Blessing>(),
		ModelDb.Card<Blessing>(),
		ModelDb.Card<Ruinance>()
	];
	// 初始遺物
	public override IReadOnlyList<RelicModel> StartingRelics => [
		ModelDb.Relic<InnerStarMap>(),
		ModelDb.Relic<StarPowerLevel>() ,
		ModelDb.Relic<StarSkillQuality>(),
		ModelDb.Relic<BasicArts>() 
	];

	// 正確的池指定方式：確保這些池類 (JiangXiaoCardPool 等) 已經正確定義
	public override CardPoolModel CardPool => ModelDb.CardPool<JiangXiaoCardPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<JiangXiaoRelicPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<JiangXiaoPotionPool>();

	public override string CustomIconTexturePath => "res://JiangXiao/images/JiangXiao/character_icon_JiangXiao2.png";
	public override string CustomCharacterSelectIconPath => "res://JiangXiao/images/JiangXiao/char_select_JiangXiao.png";
	public override string CustomCharacterSelectLockedIconPath => "res://JiangXiao/images/JiangXiao/char_select_JiangXiao_locked.png";
	public override string CustomMapMarkerPath => "res://JiangXiao/images/JiangXiao/map_marker_JiangXiao.png";
	public override Color EnergyLabelOutlineColor => Color.Color8(255, 100, 100);
	public override string CustomIconPath => "res://JiangXiao/scenes/JiangXiao/jiang_xiao_icon.tscn";
	public override string CustomVisualPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao.tscn";
	public override string CustomRestSiteAnimPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_rest_site.tscn";
	public override string CustomMerchantAnimPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_merchant.tscn";
	public override string CustomCharacterSelectBg => "res://JiangXiao/scenes/JiangXiao/char_select_bg_jiang_xiao.tscn";
	public override string CustomCharacterSelectTransitionPath => "res://JiangXiao/images/JiangXiao/transitions/jiangxiao_transition_mat.tres";
	public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
	// 多人模式-手指。
	public override string CustomArmPointingTexturePath => "res://JiangXiao/images/JiangXiao/hands/multiplayer_hand_jiangxiao_point.png";
	// 多人模式剪刀石头布-石头。 
	public override string CustomArmRockTexturePath => "res://JiangXiao/images/JiangXiao/hands/multiplayer_hand_jiangxiao_rock.png";
	// 多人模式剪刀石头布-布。
	public override string CustomArmPaperTexturePath => "res://JiangXiao/images/JiangXiao/hands/multiplayer_hand_jiangxiao_paper.png";
	// 多人模式剪刀石头布-剪刀。
	public override string CustomArmScissorsTexturePath => "res://JiangXiao/images/JiangXiao/hands/multiplayer_hand_jiangxiao_scissors.png";
	public override string CustomAttackSfx => "";
	public override string CustomCastSfx => "";
	public override string CustomDeathSfx => "";
	public override string CharacterSelectSfx => "res://JiangXiao/sound/JiangXiao_select.mp3";
	public override List<string> GetArchitectAttackVfx() =>
	[
		"vfx/vfx_attack_blunt",
		"vfx/vfx_heavy_blunt",
		"vfx/vfx_attack_slash",
		"vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
	];

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
	 	// bool IsGuardStance() => ResolveGuardStance(controller);
		var animState = new AnimState("Idle", true);
		var animator = new CreatureAnimator(animState, controller);
		animator.AddAnyState("Idle", animState);
		animator.AddAnyState("Hit", new AnimState("Hit") { NextState = animState });
		animator.AddAnyState("Attack", new AnimState("Attack") { NextState = animState });
		return animator;
	}
	// private static bool ResolveGuardStance(MegaSprite controller)
	// {
	// 		if (controller.BoundObject is not Node node)
	// 		return false;

	// 	Node? current = node;
	// 	while (current != null)
	// 	{
	// 		if (current is NCreature nCreature)
	// 			return nCreature.Entity.HasPower<StarChar>();

	// 		current = current.GetParent();
	// 	}

	// 	return false;
	// }

	public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return SetupAnimationState(controller, "Idle", hitName: "Hit");
	}
}
