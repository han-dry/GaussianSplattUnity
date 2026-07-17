using UnityEngine;
using UnityEngine.UIElements;

public class UIInputManager : MonoBehaviour
{
    [Header("Key Configurations")]
    [Tooltip("Key used to toggle between game interaction and UI cursor interaction.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("Camera Control Override")]
    [Tooltip("Drag your scene's FlyCamera or camera movement script here to disable it while using the UI.")]
    [SerializeField] private FlyCamera flyCameraScript;

    [Header("UI Reference")]
    [Tooltip("Drag your UIDocument GameObject here to control its interaction state.")]
    [SerializeField] private UIDocument uiDocument;

    private bool isUiModeActive = false;

    void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = Object.FindAnyObjectByType<UIDocument>();
        }

        if (flyCameraScript == null)
        {
            flyCameraScript = Object.FindAnyObjectByType<FlyCamera>();
        }

        // Default: start with mouse locked and UI interaction completely disabled
        isUiModeActive = false;
        SetUiMode(isUiModeActive);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isUiModeActive = !isUiModeActive;
            SetUiMode(isUiModeActive);
        }

        // Alternative: If the user clicks back onto the empty scene space, re-lock the camera
        // if (isUiModeActive && Input.GetMouseButtonDown(0))
        // {
        //     if (uiDocument != null && uiDocument.rootVisualElement != null)
        //     {
        //         Vector2 mousePos = Input.mousePosition;
        //         Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(uiDocument.rootVisualElement.panel, new Vector2(mousePos.x, Screen.height - mousePos.y));
                
        //         VisualElement picked = uiDocument.rootVisualElement.PanelPick(panelPos);
        //         // If we clicked completely outside the interactive controls box, lock back to game mode
        //         if (picked == null || picked == uiDocument.rootVisualElement)
        //         {
        //             isUiModeActive = false;
        //             SetUiMode(false);
        //         }
        //     }
        // }
    }

    private void SetUiMode(bool enableUi)
    {
        if (enableUi)
        {
            // 1. Enable hardware cursor interaction
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            // 2. Turn on UI interaction so sliders can be clicked and dragged
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                // Makes all fields interactive
                uiDocument.rootVisualElement.SetEnabled(true);

                // Set pick mode to Position so it accepts mouse clicks and keyboard focus
                uiDocument.rootVisualElement.pickingMode = PickingMode.Position;
                
                // Allow the panel to receive focus again
                uiDocument.rootVisualElement.focusable = true;
            }

            // 3. Pause camera controls to avoid looking around while dragging UI
            if (flyCameraScript != null)
            {
                flyCameraScript.enabled = false;
            }
        }
        else
        {
            // 1. Force release any active UI focus before locking
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                // Dislodge active text field focus
                uiDocument.rootVisualElement.Focus();

                // SetEnabled(false) deeply freezes all sub-fields, sliders, and text inputs.
                // It remains fully visible on screen but blocks WASD or mouse focus leakages.
                uiDocument.rootVisualElement.SetEnabled(false);
                
                // Set pick mode to Ignore so the UI becomes completely transparent to inputs
                uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
                
                // Prevent keyboard navigation/focus from entering the panel
                uiDocument.rootVisualElement.focusable = false;
            }

            // 2. Lock the hardware cursor back to the center of the game screen
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            // 3. Resume Fly Camera controls
            if (flyCameraScript != null)
            {
                flyCameraScript.enabled = true;
            }
        }
    }
}