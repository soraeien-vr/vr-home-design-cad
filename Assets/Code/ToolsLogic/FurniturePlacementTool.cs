using UnityEngine;
using UnityEngine.InputSystem;

public class FurniturePlacementTool : VRToolBase
{
    [Header("Furniture Settings")]
    [SerializeField] private GameObject selectedFurniturePrefab;
    [SerializeField] private LayerMask placementLayerMask = ~0;
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private GameObject furnitureLibraryCanvas;
    [SerializeField] private FurnitureFileMenu furnitureFileMenu;

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
    private float objectHalfHeight = 0f;

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

        // The menu button works even when the library is already open.
        if (secondaryAction.action.triggered)
        {
            ToggleFurnitureLibrary();
        }

        // Do not place furniture when the VR user presses a button inside the library.
        bool libraryOpen = furnitureFileMenu != null
            ? furnitureFileMenu.IsOpen
            : furnitureLibraryCanvas != null && furnitureLibraryCanvas.activeSelf;
        if (libraryOpen)
        {
            if (previewObject != null) previewObject.SetActive(false);
            return;
        }

        HandleRotationInput();
        UpdatePreviewPosition(rightControllerTransform);
        if (triggerAction.action.triggered) TryPlaceFurniture(rightControllerTransform);
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

            // Базовая ориентация по нормали (пол/стена) + поворот игрока по Y
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);

            // Позиция попадания + сдвиг вверх по нормали на половину высоты хитбокса
            Vector3 adjustedPosition = hit.point + (hit.normal * objectHalfHeight);

            previewObject.transform.position = adjustedPosition;
            previewObject.transform.rotation = finalRotation;
        }
        else
        {
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
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);

            // Применяем ту же логику смещения для реального спавна
            Vector3 spawnPosition = hit.point + (hit.normal * objectHalfHeight);

            // Спавним реальный объект
            GameObject spawnedObj = Instantiate(selectedFurniturePrefab, spawnPosition, finalRotation);

            FurnitureObject furniture = spawnedObj.GetComponent<FurnitureObject>();
            if (furniture == null) furniture = spawnedObj.AddComponent<FurnitureObject>();
            furniture.InitializeFurniture(
                System.Guid.NewGuid().ToString(),
                selectedFurniturePrefab.name,
                spawnPosition,
                finalRotation,
                spawnedObj.transform.localScale
            );

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

        // Вычисляем высоту объекта по его коллайдерам или рендерерам
        CalculateObjectSize(previewObject);

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

    private void CalculateObjectSize(GameObject obj)
    {
        objectHalfHeight = 0f;

        // Сначала пытаемся найти Collider (предпочтительно, так как это хитбокс)
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            // Расстояние от центра объекта до его нижней точки по локальной оси Y
            objectHalfHeight = obj.transform.InverseTransformPoint(combinedBounds.min).y;
            // Берем модуль смещения (если pivot выше минимума хитбокса)
            objectHalfHeight = Mathf.Abs(objectHalfHeight);
            return;
        }

        // Запасной вариант: считаем по Renderer (если у префаба вообще нет коллайдеров)
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            objectHalfHeight = Mathf.Abs(obj.transform.InverseTransformPoint(combinedBounds.min).y);
        }
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
        if (furnitureFileMenu != null)
        {
            furnitureFileMenu.ToggleMenu();
        }
        else if (furnitureLibraryCanvas != null)
        {
            furnitureLibraryCanvas.SetActive(!furnitureLibraryCanvas.activeSelf);
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