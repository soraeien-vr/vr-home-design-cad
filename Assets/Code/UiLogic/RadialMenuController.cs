using UnityEngine;
using UnityEngine.InputSystem; // Обязательно для InputActionProperty

public class RadialMenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public InputActionProperty toggleAction; // Ваше поле

    private bool isMenuVisible = false;

    void OnEnable()
    {
        // Принудительно включаем действие, чтобы Unity его слушала
        toggleAction.action.Enable();
        menuCanvas.SetActive(false);
    }

    void OnDisable()
    {
        toggleAction.action.Disable();
    }

    void Update()
    {
        // Проверяем, было ли действие выполнено в этом кадре
        if (toggleAction.action.triggered)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isMenuVisible = !isMenuVisible;
        menuCanvas.SetActive(isMenuVisible);
        Debug.Log("Меню бахнули");
    }
}