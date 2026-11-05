using UnityEngine;

public abstract class VRToolBase : MonoBehaviour
{
    [Header("Tool Info")]
    [SerializeField] private string toolName;
    public string ToolName => toolName;

    public virtual void OnToolSelected()
    {
        gameObject.SetActive(true);
        Debug.Log($"[Tool] Выбран инструмент: {toolName}");
    }

    public virtual void OnToolDeselected()
    {
        gameObject.SetActive(false);
        Debug.Log($"[Tool] Выключен инструмент: {toolName}");
    }

    /// <summary>
    /// Каждый инструмент переопределяет этот метод и сам решает, какие кнопки ему слушать.
    /// </summary>
    public virtual void OnToolUpdate(Transform rightControllerTransform)
    {
        // Переопределяется в дочерних классах
    }
}