using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BaseLib.Config;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
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

	public static string Localize(string key,params object[] args){
		var loc = LocString.GetIfExists(
			"settings_ui",
			$"MODLISTSORTER-{key}"
		);
		string text = loc?.GetFormattedText() ?? key;
		return args.Length > 0
			? string.Format(text, args)
			: text;
	}

	public static void Initialize()
    {
        Logger.Info("ModListSorter 模組正在初始化...");
        
        // 讀取我們的 JSON 設定檔
        SorterConfigManager.LoadConfig();

        // 註冊 UI
        ModConfigRegistry.Register(ModId, new SorterModConfig());
        
        // 啟動排序
        ExecuteModListSorting();

        Harmony harmony = new(ModId);
        harmony.PatchAll();     
        Logger.Info("ModListSorter 模組初始化與退場攔截掛載完成！");
    }

	[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.NGame), "Quit")]
	public static class NGameQuitPatch
	{
		[HarmonyPostfix]
		public static void Postfix()
		{
			MainFile.Logger.Info("[Sorter] 偵測到遊戲已完成退場存檔，開始執行終極設定檔排序...");
			MainFile.ExecuteModListSorting();
		}
	}

	public static void ExecuteModListSorting()
	{
		try
		{
			string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
			string sts2SteamPath = Path.Combine(appDataPath, "SlayTheSpire2", "Steam");

			if (!Directory.Exists(sts2SteamPath)) return;

			var saveFiles = Directory.GetFiles(sts2SteamPath, "settings.save", SearchOption.AllDirectories);
			foreach (var filePath in saveFiles) SortSingleSettingsFile(filePath);
		}
		catch (Exception ex)
		{
			Logger.Info($"[Sorter] 搜尋設定檔時發生錯誤: {ex.Message}");
		}
	}

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

				var priorityMods = SorterConfigManager.CurrentConfig.PriorityMods;

				var sortedMods = extractedMods
					.OrderBy(m => 
					{
						if (m.Id == "BaseLib") return -2; 
						if (m.Id == MainFile.ModId) return -1; 

						int index = priorityMods.IndexOf(m.Id);
						return index != -1 ? index : int.MaxValue; 
					})
					.ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
					.ToList();

				JsonArray newModList = new JsonArray();
				foreach (var mod in sortedMods)
				{
					newModList.Add(new JsonObject
					{
						["id"] = mod.Id,
						["is_enabled"] = mod.IsEnabled,
						["source"] = mod.Source
					});
				}

				modSettings["mod_list"] = newModList;
				File.WriteAllText(filePath, rootObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
				Logger.Info($"[Sorter] [成功] 已重寫並排序設定檔: {Path.GetFileName(Path.GetDirectoryName(filePath))}/settings.save");
			}
		}
		catch (Exception ex) { Logger.Info($"[Sorter] 處置設定檔時失敗: {ex.Message}"); }
	}
}