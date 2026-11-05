using UnityEngine;

public class ToolManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rightControllerTransform;

    [Header("Tools")]
    [SerializeField] private VRToolBase defaultTool;

    private VRToolBase currentTool;

    void Start()
    {
        if (defaultTool != null)
        {
            SwitchTool(defaultTool);
        }
    }

    void Update()
    {
        // Менеджер просто передает ссылку на контроллер текущему инструменту.
        // Больше никаких ограничений по кнопкам!
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

        if (currentTool != null)
            currentTool.OnToolSelected();
    }
}