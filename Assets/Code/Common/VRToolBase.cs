using UnityEngine;

public abstract class VRToolBase : MonoBehaviour
{
    [Header("Right Hand Visual")]
    [Tooltip("Префаб модели, который заменит Sphere на Right Controller при выборе этого инструмента")]
    [SerializeField] private GameObject handReplacementPrefab;

    [Header("Tool Info")]
    [SerializeField] private string toolName;

    public string ToolName => toolName;
    public GameObject HandReplacementPrefab => handReplacementPrefab;
    public bool IsSelected { get; private set; }

    public virtual void OnToolSelected()
    {
        IsSelected = true;
        gameObject.SetActive(true);
        Debug.Log($"[Tool] Выбран инструмент: {toolName}");
    }

    public virtual void OnToolDeselected()
    {
        IsSelected = false;
        gameObject.SetActive(false);
        Debug.Log($"[Tool] Выключен инструмент: {toolName}");
    }

    public virtual void OnToolUpdate(Transform rightControllerTransform) { }
}