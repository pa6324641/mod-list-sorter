using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace JiangXiaoMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	// 這裡的 ModId 非常重要，必須與你的 PCK 根目錄資料夾名稱一致
	public const string ModId = "JiangXiao"; 

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
	public const string ResPath = $"res://{ModId}";

	//public static Logger Logger { get; } = new(ModId, LogType.Generic);

	public static void Initialize()
	{
		Logger.Info("江曉模組正在初始化...");
        
		
		Harmony harmony = new(ModId);
		// 獲取當前 DLL
		var assembly = Assembly.GetExecutingAssembly();
		
		// 只保留這一行，且確保它只執行一次
		//ScriptManagerBridge.LookupScriptsInAssembly(assembly);
		
		harmony.PatchAll();		
		LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer));
        LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained));
        LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.AfterObtained));
        LogPatchStatus(harmony, typeof(TouchOfOrobas), nameof(TouchOfOrobas.SetupForPlayer));
        LogPatchStatus(harmony, typeof(TouchOfOrobas), nameof(TouchOfOrobas.AfterObtained));
        LogPatchStatus(harmony, typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), typeof(string), typeof(bool), typeof(int));
		Logger.Info("江曉模組初始化完成！");
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
