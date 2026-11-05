#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Builds each prefab in Assets/FurniturePrefabs as a separate Windows AssetBundle.</summary>
public static class FurnitureBundleBuilder
{
    private const string PrefabFolder = "Assets/FurniturePrefabs";

    [MenuItem("Tools/Furniture/Build Windows Furniture Bundles")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            EditorUtility.DisplayDialog("Furniture bundles", "Switch Build Target to Windows x86_64 before building furniture bundles.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            EditorUtility.DisplayDialog("Furniture bundles", "Create Assets/FurniturePrefabs and put furniture prefabs there first.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string simpleName = Path.GetFileNameWithoutExtension(assetPath);
            string safeName = MakeSafeName(simpleName);
            // Stable GUID suffix prevents same-named prefabs in different subfolders colliding.
            string bundleName = safeName + "_" + guid.Substring(0, 8) + ".bundle";
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = new[] { assetPath }
            });
        }
        if (builds.Count == 0)
        {
            EditorUtility.DisplayDialog("Furniture bundles", "No prefabs were found in Assets/FurniturePrefabs.", "OK");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(projectRoot, "FurnitureBundlesOutput");
        Directory.CreateDirectory(output);
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            output, builds.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        if (manifest == null)
        {
            Debug.LogError("[FurnitureBundles] Build failed. Check the Unity Console.");
            return;
        }

        string editorFurnitureFolder = Path.Combine(Application.persistentDataPath, "Furniture");
        Directory.CreateDirectory(editorFurnitureFolder);
        foreach (AssetBundleBuild build in builds)
        {
            File.Copy(Path.Combine(output, build.assetBundleName),
                Path.Combine(editorFurnitureFolder, build.assetBundleName), true);
        }
        Debug.Log("[FurnitureBundles] Built " + builds.Count + " bundles in " + output
            + "; copied to Editor play-mode folder: " + editorFurnitureFolder
            + ". For a Windows standalone build, copy .bundle files to its persistentDataPath/Furniture folder.");
        EditorUtility.DisplayDialog("Furniture bundles", "Built " + builds.Count + " Windows bundles. See Console for output paths.", "OK");
    }

    private static string MakeSafeName(string input)
    {
        StringBuilder result = new StringBuilder();
        foreach (char c in input.ToLowerInvariant())
            result.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' ? c : '_');
        return result.Length == 0 ? "furniture" : result.ToString();
    }
}
#endif
