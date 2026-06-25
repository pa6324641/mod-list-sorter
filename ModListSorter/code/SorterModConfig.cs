using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Localization;
using System.Text.Json.Nodes;
using BaseLib.Config;
using Godot;

namespace ModListSorter;

public class SorterModConfig : SimpleModConfig
{
    public static bool DummyFlag { get; set; } = true;

    private Control? _mainContainer;
    private VBoxContainer? _dynamicContainer;

    private static string T(string key, params object[] args)
    {
        var loc = LocString.GetIfExists(
            "settings_ui",
            $"MODLISTSORTER-{key}"
        );

        string text = loc?.GetFormattedText() ?? key;

        return args.Length > 0
            ? string.Format(text, args)
            : text;
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        _mainContainer = optionContainer;
        BuildDynamicUI();
    }

    private HBoxContainer CreateDynamicRow(Control leftControl, Control rightControl)
    {
        var row = new HBoxContainer();
        row.CustomMinimumSize = new Vector2(0, 64);

        leftControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        
        rightControl.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        row.AddChild(leftControl);
        row.AddChild(rightControl);

        return row;
    }

    private void BuildDynamicUI()
    {
        if (_mainContainer == null) return;

        if (GodotObject.IsInstanceValid(_dynamicContainer))
        {
            _dynamicContainer!.QueueFree();
        }

        _dynamicContainer = new VBoxContainer();
        _mainContainer.AddChild(_dynamicContainer);

        var header1 = CreateSectionHeader(T("HEADER_PRIORITY.title"), alignToTop: false, centered: false);
        _dynamicContainer.AddChild(header1);

        var currentList = SorterConfigManager.CurrentConfig.PriorityMods;
        
        foreach (var modId in currentList)
        {
            if (modId == "BaseLib")
            {
                // 【修正2】改用純粹的 Godot 原生 Label，避免 MegaLabel 查不到翻譯直接變空白
                var baseLibLabel = new Label {
                    Text = $"{modId} {T("FORCE_TOP.title")}",
                    VerticalAlignment = Godot.VerticalAlignment.Center,
                    Modulate = new Color(0.6f, 0.6f, 0.6f)
                };
                baseLibLabel.AddThemeFontSizeOverride("font_size", 28);
                
                var lockedBtn = CreateRawButtonControl(T("LOCKED.title"), () => {});
                lockedBtn.Visible = false; 
                
                var row = CreateDynamicRow(baseLibLabel, lockedBtn);
                _dynamicContainer.AddChild(row);
                continue;
            }
            
            if (modId == MainFile.ModId) continue; 

            string capturedModId = modId; 
            
            // 【修正2】改用純粹的 Godot 原生 Label
            var label = new Label {
                Text = capturedModId,
                VerticalAlignment = Godot.VerticalAlignment.Center
            };
            label.AddThemeFontSizeOverride("font_size", 28);

            var removeBtn = CreateRawButtonControl(T("REMOVE.title"), () => {
                RemoveModFromPriority(capturedModId);
            });
            
            var optionRow = CreateDynamicRow(label, removeBtn);
            _dynamicContainer.AddChild(optionRow);
        }

        var divider = new ColorRect { CustomMinimumSize = new Vector2(0, 2), Color = new Color(1, 1, 1, 0.2f) };
        _dynamicContainer.AddChild(divider);

        var header2 = CreateSectionHeader(T("HEADER_LIST.title"), alignToTop: false, centered: false);
        _dynamicContainer.AddChild(header2);

        var availableMods = GetAllAvailableModIds()
            .Where(m => !currentList.Contains(m) && m != "BaseLib" && m != MainFile.ModId)
            .ToList();

        var addRow = new HBoxContainer();
        addRow.CustomMinimumSize = new Vector2(0, 64);
        addRow.AddThemeConstantOverride("separation", 20);
        
        var dropdown = new OptionButton();
        dropdown.CustomMinimumSize = new Vector2(400, 64);
        dropdown.AddThemeFontSizeOverride("font_size", 24); 
        
        if (availableMods.Count == 0)
        {
            dropdown.AddItem(T("NO_MODS.title"));
            dropdown.Disabled = true; 
        }
        else
        {
            foreach (var mod in availableMods)
            {
                dropdown.AddItem(mod);
            }
        }
        
        dropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dropdown.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        addRow.AddChild(dropdown);

        var addBtn = CreateRawButtonControl(T("ADD.title"), () => {
            if (availableMods.Count > 0 && dropdown.Selected >= 0)
            {
                string selectedMod = dropdown.GetItemText(dropdown.Selected);
                AddModToPriority(selectedMod);
            }
        });
        addBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        
        if (availableMods.Count == 0) 
        {
            addBtn.Visible = false; 
        }

        addRow.AddChild(addBtn);

        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 12);
        marginContainer.AddThemeConstantOverride("margin_top", 12);
        marginContainer.AddThemeConstantOverride("margin_bottom", 24);
        marginContainer.AddChild(addRow);

        _dynamicContainer.AddChild(marginContainer);
        
        SimpleModConfig.SetupFocusNeighbors(_mainContainer);
    }

    private void AddModToPriority(string modId)
    {
        var list = SorterConfigManager.CurrentConfig.PriorityMods;
        if (!list.Contains(modId))
        {
            list.Add(modId);
            SorterConfigManager.SaveConfig();
            
            // 【修正1】換回標準日誌系統，避免 BaseLib 攔截並在退出時彈出 Error 視窗
            MainFile.Logger.Info(T("ADDED_MSG.title", modId));
            
            BuildDynamicUI(); 
        }
    }

    private void RemoveModFromPriority(string modId)
    {
        var list = SorterConfigManager.CurrentConfig.PriorityMods;
        if (list.Contains(modId))
        {
            list.Remove(modId);
            SorterConfigManager.SaveConfig();
            
            // 【修正1】換回標準日誌系統
            MainFile.Logger.Info(T("REMOVED_MSG.title", modId));
            
            BuildDynamicUI(); 
        }
    }

    public static List<string> GetAllAvailableModIds()
    {
        List<string> availableMods = new List<string>();
        try
        {
            string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string steamDirPath = Path.Combine(appDataPath, "SlayTheSpire2", "Steam");

            if (Directory.Exists(steamDirPath))
            {
                string[] steamIdDirs = Directory.GetDirectories(steamDirPath);
                
                foreach (var dir in steamIdDirs)
                {
                    string settingsPath = Path.Combine(dir, "settings.save");
                    
                    if (File.Exists(settingsPath))
                    {
                        string jsonContent = File.ReadAllText(settingsPath);
                        var rootNode = JsonNode.Parse(jsonContent);

                        if (rootNode is JsonObject rootObj && 
                            rootObj["mod_settings"] is JsonObject modSettings && 
                            modSettings["mod_list"] is JsonArray modList)
                        {
                            foreach (var node in modList)
                            {
                                string modId = node?["id"]?.GetValue<string>() ?? "";
                                if (!string.IsNullOrEmpty(modId)) availableMods.Add(modId);
                            }
                        }
                        
                        if (availableMods.Count > 0) break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info(T("GET_LIST_FAILED.title", ex.Message));
        }
        
        return availableMods.Distinct().OrderBy(id => id).ToList();
    }
}