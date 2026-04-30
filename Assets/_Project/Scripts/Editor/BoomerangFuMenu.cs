#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Root menu untuk semua editor automation Boomerang Fu rework.
/// Tools > Boomerang Fu > ...
/// </summary>
public static class BoomerangFuMenu
{
    private const string Root = "Tools/Boomerang Fu/";

    [MenuItem(Root + "Setup All (Run All Phases)")]
    public static void SetupAll()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup All",
            "Run all auto-setup steps? Creates SOs, audio, default configs.",
            "Yes", "Cancel")) return;

        SetupConfigs();
        SetupAudio();
        EditorAssetUtils.Refresh();
        Debug.Log("[BoomerangFu] Setup All complete.");
    }

    [MenuItem(Root + "Setup/Configs (Default SOs)")]
    public static void SetupConfigs()
    {
        const string folder = EditorAssetUtils.ProjectRoot + "/ScriptableObjects/Configs";
        EditorAssetUtils.EnsureFolder(folder);

        var player = EditorAssetUtils.CreateOrLoadSO<PlayerStatsSO>(folder + "/PlayerStats_Default.asset");
        var boom   = EditorAssetUtils.CreateOrLoadSO<BoomerangStatsSO>(folder + "/BoomerangStats_Default.asset");
        var round  = EditorAssetUtils.CreateOrLoadSO<RoundConfigSO>(folder + "/RoundConfig_Default.asset");

        EditorAssetUtils.Save(player);
        EditorAssetUtils.Save(boom);
        EditorAssetUtils.Save(round);
        EditorAssetUtils.Refresh();
        Debug.Log("[BoomerangFu] Configs created at " + folder);
    }

    [MenuItem(Root + "Setup/Audio (Mixer + Cues + Library)")]
    public static void SetupAudio()
    {
        AudioSetupAutomation.RunAll();
    }
}
#endif
