using UnityEngine;

/// <summary>
/// Класс стены — простой прямоугольный объект с настраиваемыми размерами и поддержкой текстур
/// </summary>
public class WallObject : RoomObjectBase
{
    [Header("Wall Dimensions")]
    [SerializeField] private float length = 3.0f;
    [SerializeField] private float height = 2.8f;
    [SerializeField] private float thickness = 0.2f;

    public float Length => length;
    public float Height => height;
    public float Thickness => thickness;

    /// <summary>
    /// Перегрузка инициализации специально для стены (с учетом размеров)
    /// </summary>
    public void InitializeWall(string id, string name, Vector3 position, Quaternion rotation, float length, float height, float thickness)
    {
        base.Initialize(id, name, position, rotation);

        SetDimensions(length, height, thickness);
    }

    public void SetDimensions(float newLength, float newHeight, float newThickness)
    {
        length = Mathf.Max(0.1f, newLength);
        height = Mathf.Max(0.1f, newHeight);
        thickness = Mathf.Max(0.05f, newThickness);

        UpdateWallDimensions();
    }

    private void UpdateWallDimensions()
    {
        // Масштабируем куб под размеры стены в метрах
        transform.localScale = new Vector3(thickness, height, length);
    }
}