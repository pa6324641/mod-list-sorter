using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ModListSorter;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	public const string ModId = "ModListSorter"; 

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
		new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
	public const string ResPath = $"res://{ModId}";

	public static void Initialize()
	{
		Logger.Info("ModListSorter 模組正在初始化...");
		
		// 1. 啟動時依然執行一次（防範未然）
		ExecuteModListSorting();

		// 2. 註冊 Harmony 自動補丁（這會自動將下方的 NGameQuitPatch 注入系統）
		Harmony harmony = new(ModId);
		harmony.PatchAll();		
		
		LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer));
		LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained));
		LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
		LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.AfterObtained));
		LogPatchStatus(harmony, typeof(TouchOfOrobas), nameof(TouchOfOrobas.SetupForPlayer));
		LogPatchStatus(harmony, typeof(TouchOfOrobas), nameof(TouchOfOrobas.AfterObtained));
		LogPatchStatus(harmony, typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), typeof(string), typeof(bool), typeof(int));
		
		Logger.Info("ModListSorter 模組初始化與退場攔截掛載完成！");
	}

	/// <summary>
	/// Harmony 補丁：攔截遊戲退場事件
	/// </summary>
	[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "Quit")]
	public static class NGameQuitPatch
	{
		// Postfix 代表在 NGame.Quit() 執行完畢（即遊戲寫完 settings.save）之後才觸發
		[HarmonyPostfix]
		public static void Postfix()
		{
			MainFile.Logger.Info("[Sorter] 偵測到遊戲已完成退場存檔，開始執行終極設定檔排序...");
			MainFile.ExecuteModListSorting();
		}
	}

	/// <summary>
	/// 自動尋找並排序 settings.save 中的模組清單
	/// </summary>
	public static void ExecuteModListSorting()
	{
		try
		{
			string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
			string sts2SteamPath = Path.Combine(appDataPath, "SlayTheSpire2", "Steam");

			if (!Directory.Exists(sts2SteamPath))
			{
				Logger.Info($"[Sorter] 未找到 STS2 Steam 儲存路徑: {sts2SteamPath}，跳過排序。");
				return;
			}

			var saveFiles = Directory.GetFiles(sts2SteamPath, "settings.save", SearchOption.AllDirectories);
			
			foreach (var filePath in saveFiles)
			{
				SortSingleSettingsFile(filePath);
			}
		}
		catch (Exception ex)
		{
			Logger.Info($"[Sorter] 搜尋設定檔時發生嚴重錯誤: {ex.Message}");
		}
	}

	/// <summary>
	/// 針對單一 settings.save 檔案進行讀取、排序與寫入
	/// </summary>
	private static void SortSingleSettingsFile(string filePath)
	{
		try
		{
			string jsonContent = File.ReadAllText(filePath);
			JsonNode? rootNode = JsonNode.Parse(jsonContent);

			if (rootNode is not JsonObject rootObj) return;

			if (rootObj["mod_settings"] is JsonObject modSettings && 
			    modSettings["mod_list"] is JsonArray modList)
			{
				var extractedMods = modList.Select(node => new
				{
					Id = node?["id"]?.GetValue<string>() ?? "",
					IsEnabled = node?["is_enabled"]?.GetValue<bool>() ?? false,
					Source = node?["source"]?.GetValue<string>() ?? "mods_directory"
				}).ToList();

				// BaseLib 置頂 (0)，其餘自訂角色 Mod 依字母排序 (1)
				var sortedMods = extractedMods
					.OrderBy(m => m.Id == "BaseLib" ? 0 : 1)
					.ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
					.ToList();

				JsonArray newModList = new JsonArray();
				foreach (var mod in sortedMods)
				{
					var modJsonObj = new JsonObject
					{
						["id"] = mod.Id,
						["is_enabled"] = mod.IsEnabled,
						["source"] = mod.Source
					};
					newModList.Add(modJsonObj);
				}

				modSettings["mod_list"] = newModList;

				var writeOptions = new JsonSerializerOptions { WriteIndented = true };
				string updatedJsonText = rootObj.ToJsonString(writeOptions);
				
				File.WriteAllText(filePath, updatedJsonText);
				Logger.Info($"[Sorter] [成功] 已重寫並排序設定檔: {Path.GetFileName(Path.GetDirectoryName(filePath))}/settings.save");
			}
		}
		catch (Exception ex)
		{
			Logger.Info($"[Sorter] 處置設定檔時失敗: {ex.Message}");
		}
	}

	private static void LogPatchStatus(Harmony harmony, Type type, string methodName)
	{
		var method = AccessTools.Method(type, methodName);
		LogPatchStatus(harmony, method, type, methodName);
	}

	private static void LogPatchStatus(Harmony harmony, Type type, string methodName, params Type[] argumentTypes)
	{
		var method = AccessTools.Method(type, methodName, argumentTypes);
		LogPatchStatus(harmony, method, type, $"{methodName}({string.Join(",", argumentTypes.Select(t => t.Name))})");
	}

	private static void LogPatchStatus(Harmony harmony, System.Reflection.MethodInfo? method, Type type, string methodName)
	{
		if (method == null)
		{
			Logger.Info($"[Harmony] target not found: {type.FullName}.{methodName}");
			return;
		}

		var patchInfo = Harmony.GetPatchInfo(method);
		var mine = patchInfo == null
			? 0
			: patchInfo.Prefixes.Count(p => p.owner == harmony.Id) +
			  patchInfo.Postfixes.Count(p => p.owner == harmony.Id) +
			  patchInfo.Transpilers.Count(p => p.owner == harmony.Id) +
			  patchInfo.Finalizers.Count(p => p.owner == harmony.Id);

		Logger.Info($"[Harmony] {type.Name}.{methodName}: patchedByMe={mine}");
	}
}