using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Browses a Windows directory containing furniture files. Does not load 3D models.
/// Add this component to an active GameObject; assign the menu and UI references in the Inspector.
/// </summary>
public sealed class FurnitureFileMenu : MonoBehaviour
{
    [Serializable]
    public sealed class FurnitureFileSelectedEvent : UnityEvent<string> { }

    [Header("Folder")]
    [Tooltip("Leave empty to use <Application.persistentDataPath>/Furniture")]
    [SerializeField] private string rootFolder = "";
    [SerializeField] private bool includeAssetBundles = true;
    [SerializeField] private bool includeGlb = false;
    [SerializeField] private bool includeGltf = false;
    [SerializeField] private bool includeFbx = false;
    [SerializeField] private bool includeObj = false;

    [Header("UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform listContent;
    [SerializeField] private FurnitureFileMenuItem itemPrefab;
    [SerializeField] private TMP_Text pathLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;

    [Header("Actions")]
    [SerializeField] private FurnitureFileSelectedEvent onFurnitureFileSelected = new FurnitureFileSelectedEvent();

    private readonly List<FurnitureFileMenuItem> spawnedItems = new List<FurnitureFileMenuItem>();
    private string rootFullPath;
    private string currentFolder;

    public bool IsOpen => menuPanel != null && menuPanel.activeSelf;
    public string RootFolder => rootFullPath;
    public string CurrentFolder => currentFolder;
    public FurnitureFileSelectedEvent OnFurnitureFileSelected => onFurnitureFileSelected;

    private void Awake()
    {
        rootFullPath = Path.GetFullPath(string.IsNullOrWhiteSpace(rootFolder)
            ? Path.Combine(Application.persistentDataPath, "Furniture")
            : Environment.ExpandEnvironmentVariables(rootFolder));
        currentFolder = rootFullPath;

        if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);
        if (backButton != null) backButton.onClick.AddListener(GoBack);

        Debug.Log($"{rootFullPath} and {RootFolder}");
    }

    private void OnDestroy()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveListener(Refresh);
        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
    }

    public void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        Refresh();
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        if (menuPanel.activeSelf) CloseMenu();
        else OpenMenu();
    }

    public void Refresh()
    {
        ClearItems();

        if (listContent == null || itemPrefab == null)
        {
            SetStatus("Assign List Content and Item Prefab in Inspector.");
            return;
        }

        try
        {
            Directory.CreateDirectory(rootFullPath);
            if (!Directory.Exists(currentFolder) || !IsWithinRoot(currentFolder))
                currentFolder = rootFullPath;

            string[] folders = Directory.GetDirectories(currentFolder);
            string[] files = Directory.GetFiles(currentFolder);
            Array.Sort(folders, StringComparer.OrdinalIgnoreCase);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            int count = 0;
            foreach (string folder in folders)
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) continue;
                string captured = folder;
                AddItem(Path.GetFileName(folder), true, () => OpenFolder(captured));
                count++;
            }

            foreach (string file in files)
            {
                if (!IsSupportedModel(file)) continue;
                string captured = file;
                AddItem(Path.GetFileName(file), false, () => ChooseFile(captured));
                count++;
            }

            if (pathLabel != null)
            {
                string relative = currentFolder.Substring(rootFullPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                pathLabel.text = string.IsNullOrEmpty(relative) ? "Furniture" : "Furniture / " + relative.Replace('\\', '/');
            }
            if (backButton != null) backButton.interactable = !PathsEqual(currentFolder, rootFullPath);
            SetStatus(count == 0 ? "No furniture files or folders." : count + " item(s)");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            SetStatus("Cannot read folder: " + exception.Message);
            Debug.LogWarning("FurnitureFileMenu: " + exception);
        }
    }

    public void GoBack()
    {
        if (PathsEqual(currentFolder, rootFullPath)) return;
        DirectoryInfo parent = Directory.GetParent(currentFolder);
        currentFolder = parent != null && IsWithinRoot(parent.FullName) ? parent.FullName : rootFullPath;
        Refresh();
    }

    private void OpenFolder(string folder)
    {
        if (!IsWithinRoot(folder)) return;
        currentFolder = folder;
        Refresh();
    }

    private void ChooseFile(string file)
    {
        if (!IsWithinRoot(file) || !File.Exists(file))
        {
            Refresh();
            return;
        }

        // Listener (FurnitureAssetBundleSelector.SelectFile) loads a prefab and calls
        // FurniturePlacementTool.SetSelectedFurniture(GameObject).
        onFurnitureFileSelected.Invoke(file);
        SetStatus("Selected: " + Path.GetFileName(file));
    }

    private bool IsSupportedModel(string file)
    {
        string extension = Path.GetExtension(file).ToLowerInvariant();
        return (includeAssetBundles && extension == ".bundle") ||
               (includeGlb && extension == ".glb") ||
               (includeGltf && extension == ".gltf") ||
               (includeFbx && extension == ".fbx") ||
               (includeObj && extension == ".obj");
    }

    private void AddItem(string displayName, bool folder, UnityAction action)
    {
        FurnitureFileMenuItem item = Instantiate(itemPrefab, listContent);
        item.Setup(displayName, folder, action);
        spawnedItems.Add(item);
    }

    private void ClearItems()
    {
        foreach (FurnitureFileMenuItem item in spawnedItems)
            if (item != null) Destroy(item.gameObject);
        spawnedItems.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
    }

    private bool IsWithinRoot(string path)
    {
        string full = Path.GetFullPath(path);
        string rootWithSeparator = rootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return PathsEqual(full, rootFullPath) || full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
