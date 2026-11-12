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

        // Кнопка меню работает даже если библиотека уже открыта
        if (secondaryAction.action.triggered)
        {
            ToggleFurnitureLibrary();
        }

        // Не даем ставить мебель, если открыто меню библиотеки
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
        float inputX = 0f;

        if (rotateAction.action.activeControl?.valueType == typeof(Vector2))
        {
            inputX = rotateAction.action.ReadValue<Vector2>().x;
        }
        else
        {
            inputX = rotateAction.action.ReadValue<float>();
        }

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

            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);

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

            Vector3 spawnPosition = hit.point + (hit.normal * objectHalfHeight);

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

        previewObject = Instantiate(selectedFurniturePrefab);
        previewObject.name = "GhostPreview_" + selectedFurniturePrefab.name;

        CalculateObjectSize(previewObject);

        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

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

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            objectHalfHeight = obj.transform.InverseTransformPoint(combinedBounds.min).y;
            objectHalfHeight = Mathf.Abs(objectHalfHeight);
            return;
        }

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
            // Проверяем, закрыто ли меню в данный момент
            if (!furnitureFileMenu.IsOpen)
            {
                PositionMenuInFrontOfUser();
                furnitureFileMenu.OpenMenu();
            }
            else
            {
                furnitureFileMenu.CloseMenu();
            }
        }
        else if (furnitureLibraryCanvas != null)
        {
            bool willBeActive = !furnitureLibraryCanvas.activeSelf;
            if (willBeActive)
            {
                PositionCanvasInFrontOfUser(furnitureLibraryCanvas);
            }
            furnitureLibraryCanvas.SetActive(willBeActive);
        }
    }

    /// <summary>
    /// Переносит меню перед глазами игрока перед открытием
    /// </summary>
    private void PositionMenuInFrontOfUser()
    {
        // Пытаемся найти камеру игрока (XR Camera / Main Camera)
        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        if (camTransform == null) return;

        // Берем корневой объект меню (трансформ компонента furnitureFileMenu)
        Transform menuTransform = furnitureFileMenu.transform;

        // Ставим меню на расстоянии 1.5 метра перед лицом игрока на уровне глаз
        Vector3 targetPosition = camTransform.position + camTransform.forward * 1.5f;
        menuTransform.position = targetPosition;

        // Поворачиваем меню лицом к игроку
        Vector3 lookDirection = menuTransform.position - camTransform.position;
        lookDirection.y = 0; // Оставляем меню вертикальным, убирая наклон вверх/вниз
        if (lookDirection != Vector3.zero)
        {
            menuTransform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void PositionCanvasInFrontOfUser(GameObject canvasObj)
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        if (camTransform == null) return;

        Vector3 targetPosition = camTransform.position + camTransform.forward * 1.5f;
        canvasObj.transform.position = targetPosition;

        Vector3 lookDirection = canvasObj.transform.position - camTransform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            canvasObj.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public void SetSelectedFurniture(GameObject newPrefab)
    {
        Debug.Log($"[FurnitureTool] Выбран предмет: {newPrefab.name}");
        selectedFurniturePrefab = newPrefab;

        if (gameObject.activeSelf)
        {
            CreatePreview();
        }
    }
}