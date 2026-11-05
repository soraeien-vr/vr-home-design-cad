using System;
using System.IO;
using UnityEngine;

/// <summary>Loads a GameObject prefab from a Windows AssetBundle and selects it for placement.</summary>
public sealed class FurnitureAssetBundleSelector : MonoBehaviour
{
    [SerializeField] private FurniturePlacementTool placementTool;
    [SerializeField] private FurnitureFileMenu fileMenu;

    public void SelectFile(string fullPath)
    {
        if (placementTool == null)
        {
            Debug.LogError("[FurnitureLibrary] Assign FurniturePlacementTool in Inspector.");
            return;
        }
        if (!File.Exists(fullPath) || !string.Equals(Path.GetExtension(fullPath), ".bundle", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[FurnitureLibrary] Missing or unsupported AssetBundle: " + fullPath);
            return;
        }

        AssetBundle bundle = null;
        try
        {
            bundle = AssetBundle.LoadFromFile(fullPath);
            if (bundle == null)
            {
                Debug.LogError("[FurnitureLibrary] Cannot open bundle (check Windows platform and build version): " + fullPath);
                return;
            }

            GameObject prefab = null;
            string[] names = bundle.GetAllAssetNames();
            for (int i = 0; i < names.Length; i++)
            {
                GameObject candidate = bundle.LoadAsset<GameObject>(names[i]);
                if (candidate == null) continue;
                if (prefab != null)
                {
                    Debug.LogError("[FurnitureLibrary] Expected exactly one GameObject prefab per bundle: " + fullPath);
                    return;
                }
                prefab = candidate;
            }

            if (prefab == null)
            {
                Debug.LogError("[FurnitureLibrary] Bundle does not contain a GameObject: " + fullPath);
                return;
            }

            placementTool.SetSelectedFurniture(prefab);
            if (fileMenu != null) fileMenu.CloseMenu();
            Debug.Log("[FurnitureLibrary] Selected: " + prefab.name);
        }
        catch (Exception e)
        {
            Debug.LogError("[FurnitureLibrary] Could not load furniture: " + e);
        }
        finally
        {
            // Unload the bundle's container, keeping loaded asset objects used as prefab templates.
            if (bundle != null) bundle.Unload(false);
        }
    }
}
