using System.IO;
using _Project.Develop.Infrastructure.EntryPoints;
using UnityEditor;
using UnityEngine;

internal static class ClearSavesMenu
{
    private const string MenuPath = "Tools/Clear Saves";

    [MenuItem(MenuPath)]
    private static void Clear()
    {
        string path = CheckpointService.SavePath;
        File.Delete(path + ".tmp");
        File.Delete(path);
        Debug.Log("Checkpoint cleared: " + path);
    }

    [MenuItem(MenuPath, true)]
    private static bool CanClear() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
