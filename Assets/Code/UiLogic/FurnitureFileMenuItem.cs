using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Attach to the prefab used for each folder/file row in FurnitureFileMenu.</summary>
[RequireComponent(typeof(Button))]
public sealed class FurnitureFileMenuItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text typeLabel;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(string displayName, bool isFolder, UnityAction onClick)
    {
        if (button == null) button = GetComponent<Button>();
        if (nameLabel != null) nameLabel.text = displayName;
        if (typeLabel != null) typeLabel.text = isFolder ? "[DIR]" : "[3D]";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }
}
