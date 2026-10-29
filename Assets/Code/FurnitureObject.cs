using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс для мебели и элементов интерьера с поддержкой позиции, поворота и масштабирования
/// </summary>
public class FurnitureObject : RoomObjectBase
{
    [Header("Furniture Specifics")]
    [SerializeField] private bool isDraggableInVR = true;
    [SerializeField] private Vector3 currentScale = Vector3.one;

    public bool IsDraggableInVR => isDraggableInVR;
    public Vector3 CurrentScale => currentScale;

    /// <summary>
    /// Расширенная инициализация мебели (с учетом скейла)
    /// </summary>
    public void InitializeFurniture(string id, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        base.Initialize(id, name, position, rotation);

        SetScale(scale);
    }

    /// <summary>
    /// Переопределяем применение данных о трансформации, чтобы поддерживать скейл
    /// </summary>
    public override void ApplyTransformData(Vector3 position, Quaternion rotation)
    {
        base.ApplyTransformData(position, rotation);
        transform.localScale = currentScale;
    }

    /// <summary>
    /// Метод для изменения масштаба мебели (например, уменьшить в 2 раза: SetScale(Vector3.one * 0.5f))
    /// </summary>
    public void SetScale(Vector3 newScale)
    {
        currentScale = new Vector3(
            Mathf.Max(0.01f, newScale.x),
            Mathf.Max(0.01f, newScale.y),
            Mathf.Max(0.01f, newScale.z)
        );

        transform.localScale = currentScale;
        Debug.Log($"Furniture '{ObjectName}' scale updated to: {currentScale}");
    }

    /// <summary>
    /// Удобный метод для быстрого равномерного изменения размера (например, множитель 0.5 для уменьшения в 2 раза)
    /// </summary>
    public void UniformScale(float scaleMultiplier)
    {
        SetScale(currentScale * scaleMultiplier);
    }

    /// <summary>
    /// Поворот мебели на заданный угол (удобно для VR-контроллеров)
    /// </summary>
    public void Rotate(float angleStep)
    {
        transform.Rotate(Vector3.up, angleStep);
    }
}
