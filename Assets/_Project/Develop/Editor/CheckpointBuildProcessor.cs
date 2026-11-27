using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Data;
using _Project.Develop.Gameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

public class CheckpointBuildProcessor : IProcessSceneWithReport, IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null) return;
        var ids = new HashSet<string>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var entity in root.GetComponentsInChildren<SavableBehaviour>(true))
            if (string.IsNullOrEmpty(entity.SaveId) || !ids.Add(entity.SaveId))
                throw new BuildFailedException($"Invalid checkpoint ID in {scene.path} on {entity.name}");
    }

    public void OnPreprocessBuild(BuildReport report) => RefreshCatalog();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.projectChanged += RefreshCatalog;
        EditorApplication.delayCall += RefreshCatalog;
    }

    private static void RefreshCatalog()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var asset = CheckpointCatalog.Instance;
        if (!asset) return;
        var catalog = new SerializedObject(asset);
        var entries = catalog.FindProperty("entries");
        var keys = new[] { "t:Quest", "t:Clue", "t:Hypothesis" }
            .SelectMany(filter => AssetDatabase.FindAssets(filter)).Distinct().OrderBy(key => key).ToArray();
        entries.arraySize = keys.Length;
        for (var i = 0; i < keys.Length; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("key").stringValue = keys[i];
            entry.FindPropertyRelative("asset").objectReferenceValue =
                AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(keys[i]));
        }
        if (catalog.ApplyModifiedPropertiesWithoutUndo()) AssetDatabase.SaveAssetIfDirty(asset);
    }
}
