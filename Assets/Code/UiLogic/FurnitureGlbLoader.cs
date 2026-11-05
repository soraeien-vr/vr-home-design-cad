using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

/// <summary>
/// Loads user-supplied .glb files from the Windows file system while the app is running.
/// Uses glTFast (com.unity.cloud.gltfast). The loaded model becomes a reusable inactive
/// GameObject template for FurniturePlacementTool.SetSelectedFurniture(GameObject).
/// </summary>
public sealed class FurnitureGlbLoader : MonoBehaviour
{
    [SerializeField] private FurniturePlacementTool placementTool;
    [SerializeField] private FurnitureFileMenu fileMenu;

    // Imported models and their glTF owners are retained until this component is destroyed,
    // because the placement tool creates multiple copies of the chosen model over time.
    private readonly List<GameObject> templates = new List<GameObject>();
    private readonly List<GltfImport> imports = new List<GltfImport>();
    private bool isLoading;
    private bool shuttingDown;

    public async void SelectFile(string fullPath)
    {
        if (isLoading || shuttingDown)
        {
            Debug.LogWarning("[FurnitureGlbLoader] Another model is loading.");
            return;
        }
        if (placementTool == null)
        {
            Debug.LogError("[FurnitureGlbLoader] Assign FurniturePlacementTool in Inspector.");
            return;
        }
        if (string.IsNullOrEmpty(fullPath) ||
            !string.Equals(Path.GetExtension(fullPath), ".glb", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(fullPath))
        {
            Debug.LogWarning("[FurnitureGlbLoader] Missing or unsupported file: " + fullPath);
            return;
        }
        if (fileMenu == null || !IsInsideFolder(fullPath, fileMenu.RootFolder))
        {
            Debug.LogError("[FurnitureGlbLoader] File must be within the library folder.");
            return;
        }

        isLoading = true;
        GltfImport importer = null;
        GameObject template = null;
        try
        {
            importer = new GltfImport();
            // Loads directly from the Windows file path, not the Unity AssetDatabase.
            bool loaded = await importer.LoadFile(fullPath);
            if (shuttingDown) return;
            if (!loaded)
            {
                Debug.LogError("[FurnitureGlbLoader] Failed to load GLB: " + fullPath);
                return;
            }

            template = new GameObject(Path.GetFileNameWithoutExtension(fullPath));
            template.SetActive(false); // Never display the template itself in the room.
            bool instantiated = await importer.InstantiateMainSceneAsync(template.transform);
            if (shuttingDown) return;
            if (!instantiated)
            {
                Debug.LogError("[FurnitureGlbLoader] Failed to instantiate GLB: " + fullPath);
                return;
            }

            // GLB files contain geometry/materials but usually no Unity physics colliders.
            // This collider is optional for raycast interactions with the placed furniture.
            EnsureBoxCollider(template);
            templates.Add(template);
            imports.Add(importer);
            importer = null; // ownership transferred to imports
            template = null; // ownership transferred to templates

            placementTool.SetSelectedFurniture(templates[templates.Count - 1]);
            fileMenu.CloseMenu();
            Debug.Log("[FurnitureGlbLoader] Furniture selected: " + fullPath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[FurnitureGlbLoader] Failed: " + exception);
        }
        finally
        {
            if (template != null) Destroy(template);
            if (importer != null) importer.Dispose();
            isLoading = false;
        }
    }

    private static bool IsInsideFolder(string path, string folder)
    {
        if (string.IsNullOrEmpty(folder)) return false;
        string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureBoxCollider(GameObject template)
    {
        Renderer[] renderers = template.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        // Bounds are world-space. Convert all eight corners to template local space.
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
        {
            Vector3 corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                                         y == 0 ? bounds.min.y : bounds.max.y,
                                         z == 0 ? bounds.min.z : bounds.max.z);
            Vector3 local = template.transform.InverseTransformPoint(corner);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }
        BoxCollider box = template.AddComponent<BoxCollider>();
        box.center = (min + max) * 0.5f;
        box.size = max - min;
    }

    private void OnDestroy()
    {
        shuttingDown = true;
        foreach (GameObject template in templates) if (template != null) Destroy(template);
        foreach (GltfImport importer in imports) importer.Dispose();
        templates.Clear();
        imports.Clear();
    }
}
