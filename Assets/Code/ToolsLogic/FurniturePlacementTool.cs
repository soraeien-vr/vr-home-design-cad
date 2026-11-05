using UnityEngine;
using UnityEngine.InputSystem;

public class FurniturePlacementTool : VRToolBase
{
    [Header("Furniture Settings")]
    [SerializeField] private GameObject selectedFurniturePrefab;
    [SerializeField] private LayerMask placementLayerMask = ~0;
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private GameObject furnitureLibraryCanvas;

    [Header("Preview Settings")]
    [Tooltip("Полупрозрачный материал для визуализации призрака/предпросмотра")]
    [SerializeField] private Material previewMaterial;
    [Tooltip("Скорость вращения объекта стиком/тачпадом")]
    [SerializeField] private float rotationSpeed = 120f;

    [Header("Tool Specific Inputs")]
    [SerializeField] private InputActionProperty triggerAction;     // Курок для установки
    [SerializeField] private InputActionProperty secondaryAction;   // Кнопка для библиотеки
    [SerializeField] private InputActionProperty rotateAction;      // Vector2 или Axis (стик/тачпад по горизонтали)

    private GameObject previewObject;
    private float currentYRotation = 0f;

    public override void OnToolSelected()
    {
        base.OnToolSelected();

        // Включаем действия ввода
        triggerAction.action.Enable();
        secondaryAction.action.Enable();
        rotateAction.action.Enable();

        // Создаем предпросмотр при активации инструмента
        CreatePreview();
    }

    public override void OnToolDeselected()
    {
        base.OnToolDeselected();

        // Выключаем действия
        triggerAction.action.Disable();
        secondaryAction.action.Disable();
        rotateAction.action.Disable();

        // Удаляем предпросмотр при выключении инструмента
        DestroyPreview();
    }

    public override void OnToolUpdate(Transform rightControllerTransform)
    {
        base.OnToolUpdate(rightControllerTransform);

        // 1. Считываем вращение со стика / тачпада по горизонтали
        HandleRotationInput();

        // 2. Обновляем позицию предпросмотра
        UpdatePreviewPosition(rightControllerTransform);

        // 3. Установка объекта на курок
        if (triggerAction.action.triggered)
        {
            TryPlaceFurniture(rightControllerTransform);
        }

        // 4. Открытие библиотеки
        if (secondaryAction.action.triggered)
        {
            ToggleFurnitureLibrary();
        }
    }

    private void HandleRotationInput()
    {
        // Поддерживаем как Vector2 (значение стика/тачпада), так и float (ось)
        float inputX = 0f;

        if (rotateAction.action.activeControl?.valueType == typeof(Vector2))
        {
            inputX = rotateAction.action.ReadValue<Vector2>().x;
        }
        else
        {
            inputX = rotateAction.action.ReadValue<float>();
        }

        // Вращаем предпросмотр по оси Y, если стик отклонен
        if (Mathf.Abs(inputX) > 0.1f)
        {
            currentYRotation += inputX * rotationSpeed * Time.deltaTime;
        }
    }

    private void UpdatePreviewPosition(Transform controllerTransform)
    {
        if (previewObject == null) return;

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayerMask))
        {
            if (!previewObject.activeSelf) previewObject.SetActive(true);

            // Позиция точки попадания луча
            previewObject.transform.position = hit.point;

            // Базовая ориентация по нормали (пол/стена) + дополнительный поворот от игрока по Y
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            previewObject.transform.rotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);
        }
        else
        {
            // Прячем предпросмотр, если луч улетает в пустоту
            if (previewObject.activeSelf) previewObject.SetActive(false);
        }
    }

    private void TryPlaceFurniture(Transform controllerTransform)
    {
        if (selectedFurniturePrefab == null)
        {
            Debug.LogWarning("[FurnitureTool] Не выбран префаб мебели!");
            return;
        }

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayerMask))
        {
            Vector3 spawnPosition = hit.point;
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);

            // Спавним реальный объект
            GameObject spawnedObj = Instantiate(selectedFurniturePrefab, spawnPosition, finalRotation);

            FurnitureObject furniture = spawnedObj.GetComponent<FurnitureObject>();
            if (furniture != null)
            {
                furniture.Initialize(
                    id: System.Guid.NewGuid().ToString(),
                    name: selectedFurniturePrefab.name,
                    position: spawnPosition,
                    rotation: finalRotation
                );
            }

            Debug.Log($"[FurnitureTool] Поставлено: {selectedFurniturePrefab.name}");
        }
    }

    private void CreatePreview()
    {
        DestroyPreview();

        if (selectedFurniturePrefab == null) return;

        // Инстанциируем визуальную копию
        previewObject = Instantiate(selectedFurniturePrefab);
        previewObject.name = "GhostPreview_" + selectedFurniturePrefab.name;

        // Отключаем все коллайдеры, чтобы призрак не мешал рейкастам
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Если задан прозрачный материал, применяем его к мешам
        if (previewMaterial != null)
        {
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                Material[] materials = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }
                rend.materials = materials;
            }
        }

        previewObject.SetActive(false);
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    private void ToggleFurnitureLibrary()
    {
        if (furnitureLibraryCanvas != null)
        {
            bool isActive = furnitureLibraryCanvas.activeSelf;
            furnitureLibraryCanvas.SetActive(!isActive);
        }
    }

    /// <summary>
    /// Вызывается при выборе нового объекта из будущей библиотеки
    /// </summary>
    public void SetSelectedFurniture(GameObject newPrefab)
    {
        Debug.Log($"[FurnitureTool] Открыто меню.");
        selectedFurniturePrefab = newPrefab;

        // Пересоздаем призрак под новую выбранную модель
        if (gameObject.activeSelf)
        {
            CreatePreview();
        }
    }
}