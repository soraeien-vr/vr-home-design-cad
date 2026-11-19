using UnityEngine;

public class ToolManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rightControllerTransform;

    [Tooltip("Стандартная модель руки внутри Right Controller (сейчас это объект Sphere)")]
    [SerializeField] private GameObject defaultRightHandVisual;

    [Header("Tools")]
    [SerializeField] private VRToolBase defaultTool;

    private VRToolBase currentTool;
    private GameObject currentHandModel;

    void Start()
    {
        if (defaultTool != null)
        {
            SwitchTool(defaultTool);
        }
    }

    void Update()
    {
        if (currentTool != null && rightControllerTransform != null)
        {
            currentTool.OnToolUpdate(rightControllerTransform);
        }
    }

    public void SwitchTool(VRToolBase newTool)
    {
        if (currentTool == newTool) return;

        if (currentTool != null)
            currentTool.OnToolDeselected();

        currentTool = newTool;

        ReplaceRightHandModel(currentTool);

        if (currentTool != null)
            currentTool.OnToolSelected();
    }

    private void ReplaceRightHandModel(VRToolBase tool)
    {
        // Удаляем модель предыдущего инструмента с контроллера
        if (currentHandModel != null)
        {
            Destroy(currentHandModel);
            currentHandModel = null;
        }

        // Если у нового тула задан свой префаб — прячем Sphere и спавним модель тула прямо в Right Controller
        if (tool != null && tool.HandReplacementPrefab != null && rightControllerTransform != null)
        {
            if (defaultRightHandVisual != null)
                defaultRightHandVisual.SetActive(false);

            currentHandModel = Instantiate(tool.HandReplacementPrefab, rightControllerTransform);
            currentHandModel.transform.localPosition = Vector3.zero;
            currentHandModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Если префаб не задан — возвращаем стандартную Sphere
            if (defaultRightHandVisual != null)
                defaultRightHandVisual.SetActive(true);
        }
    }
}