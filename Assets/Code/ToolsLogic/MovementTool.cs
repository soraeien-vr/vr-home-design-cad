using UnityEngine;
using UnityEngine.InputSystem;

public class MovementTool : VRToolBase
{
    [Header("Movement Settings")]
    [Tooltip("—сылка на объект игрока (обычно PersonPov или Camera Offset), который нужно перемещать")]
    [SerializeField] private Transform playerRoot;

    [SerializeField] private LayerMask teleportLayerMask = ~0; // —лои, куда можно телепортироватьс€ (например, Plane/пол)
    [SerializeField] private float maxTeleportDistance = 30f;

    [Header("Input Actions")]
    [Tooltip(" урок дл€ подтверждени€ телепортации в точку луча")]
    [SerializeField] private InputActionProperty teleportAction;

    [Header("Teleport Visual (Optional)")]
    [SerializeField] private LineRenderer laserLine; // ≈сли хотим рисовать луч телепортации

    public override void OnToolSelected()
    {
        base.OnToolSelected();
        teleportAction.action.Enable();

        if (laserLine != null) laserLine.enabled = true;
    }

    public override void OnToolDeselected()
    {
        base.OnToolDeselected();
        teleportAction.action.Disable();

        if (laserLine != null) laserLine.enabled = false;
    }

    public override void OnToolUpdate(Transform rightControllerTransform)
    {
        base.OnToolUpdate(rightControllerTransform);

        // –исуем простенький луч (если подключен LineRenderer)
        UpdateLaserPointer(rightControllerTransform);

        // ѕо нажатию курка телепортируем игрока
        if (teleportAction.action.triggered)
        {
            TryTeleport(rightControllerTransform);
        }
    }

    private void UpdateLaserPointer(Transform controllerTransform)
    {
        if (laserLine == null) return;

        laserLine.SetPosition(0, controllerTransform.position);

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxTeleportDistance, teleportLayerMask))
        {
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            laserLine.SetPosition(1, controllerTransform.position + controllerTransform.forward * 5f);
        }
    }

    private void TryTeleport(Transform controllerTransform)
    {
        if (playerRoot == null)
        {
            Debug.LogWarning("[MovementTool] Ќе указан Player Root дл€ перемещени€!");
            return;
        }

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxTeleportDistance, teleportLayerMask))
        {
            // ¬ычисл€ем смещение камеры относительно центра игрока (XR Rig / PersonPov),
            // чтобы при телепортации камера встала ровно в точку удара луча, а не центр игрока.
            Vector3 cameraOffset = Camera.main.transform.position - playerRoot.position;
            cameraOffset.y = 0; // »гнорируем разницу по высоте головы

            Vector3 targetPosition = hit.point - cameraOffset;

            // ѕеремещаем игрока
            playerRoot.position = targetPosition;

            Debug.Log($"[MovementTool] “елепортаци€ в точку: {hit.point}");
        }
    }
}