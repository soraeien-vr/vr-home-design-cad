using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Базовый абстрактный класс для всех размещаемых объектов в CAD (стены, мебель и т.д.)
/// </summary>
public abstract class RoomObjectBase : MonoBehaviour
{
    [Header("Base CAD Properties")]
    [SerializeField] private string objectId;
    [SerializeField] private string objectName;
    [SerializeField] private RoomObjectType objectType;

    [Header("Texture & Materials")]
    [SerializeField] protected Renderer targetRenderer;
    [SerializeField] private string texturePropertyName = "_BaseMap";

    public string ObjectId => objectId;
    public string ObjectName => objectName;
    public RoomObjectType Type => objectType;

    /// <summary>
    /// Базовая инициализация общих параметров
    /// </summary>
    public virtual void Initialize(string id, string name, Vector3 position, Quaternion rotation)
    {
        objectId = string.IsNullOrEmpty(id) ? System.Guid.NewGuid().ToString() : id;
        objectName = name;

        ApplyTransformData(position, rotation);

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public virtual void ApplyTransformData(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    /// <summary>
    /// Динамическая загрузка и применение новой текстуры на объект
    /// </summary>
    public virtual void ApplyTexture(Texture2D newTexture)
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"[RoomObjectBase] Renderer not found on {objectName}, cannot apply texture.");
            return;
        }

        if (newTexture == null)
        {
            Debug.LogWarning($"[RoomObjectBase] Provided texture is null for {objectName}.");
            return;
        }

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(propBlock);

        propBlock.SetTexture(texturePropertyName, newTexture);

        targetRenderer.SetPropertyBlock(propBlock);

        Debug.Log($"Applied new texture '{newTexture.name}' to object '{objectName}'.");
    }

    public virtual void OnSelected()
    {
        Debug.Log($"Selected object: {objectName} (ID: {objectId})");
    }

    public virtual void OnDeselected()
    {
        Debug.Log($"Deselected object: {objectName}");
    }
}
