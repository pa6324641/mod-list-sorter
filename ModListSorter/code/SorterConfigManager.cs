using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ModListSorter;

/// <summary>
/// 負責管理 ModListSorter 專屬的 settings.json 配置檔
/// </summary>
public static class SorterConfigManager
{
    // 定義設定檔存放路徑 (放在 SlayTheSpire2/Steam 目錄下，與 settings.save 同層方便管理)
    public static string ConfigPath 
    {
        get 
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "SlayTheSpire2", "Steam", "ModListSorter_Settings.json");
        }
    }

    // 設定檔的資料結構
    public class ConfigData
    {
        // 玩家自訂的優先級模組列表，預設必定包含 BaseLib
        public List<string> PriorityMods { get; set; } = new List<string> { "BaseLib" };
    }

    public static ConfigData CurrentConfig { get; private set; } = new ConfigData();

    /// <summary>
    /// 讀取配置檔，若不存在則生成一個帶有預設值的檔案
    /// </summary>
    public static void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string jsonContent = File.ReadAllText(ConfigPath);
                CurrentConfig = JsonSerializer.Deserialize<ConfigData>(jsonContent) ?? new ConfigData();
            }
            else
            {
                MainFile.Logger.Info("[Sorter] 尚未找到自訂設定檔，正在生成預設 ModListSorter_Settings.json...");
                SaveConfig();
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[Sorter] 讀取設定檔失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 玩家在 UI 調整完畢後，呼叫此方法將新排序寫入檔案
    /// </summary>
    public static void SaveConfig()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonContent = JsonSerializer.Serialize(CurrentConfig, options);
            File.WriteAllText(ConfigPath, jsonContent);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[Sorter] 儲存設定檔失敗: {ex.Message}");
        }
    }
}