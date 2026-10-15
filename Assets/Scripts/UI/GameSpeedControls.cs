using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public sealed class GameSpeedControls : MonoBehaviour
{
    public static float SimulationSpeed { get; private set; } = 1f;

    [SerializeField] private Button pauseButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button doubleButton;
    [SerializeField] private Button quadrupleButton;
    [SerializeField] private GameObject pauseSelected;
    [SerializeField] private GameObject normalSelected;
    [SerializeField] private GameObject doubleSelected;
    [SerializeField] private GameObject quadrupleSelected;
    [SerializeField] private GameObject pauseGlyph;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Color activeLabelColor = new Color(1f,.78f,.35f,1f);
    [SerializeField] private Color inactiveLabelColor = new Color(.47f,.41f,.55f,1f);

    public float CurrentSpeed { get; private set; } = 1f;
    private float speedBeforePause = 1f;

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
        }
        Bind(normalButton, 1f);
        Bind(doubleButton, 3f);
        Bind(quadrupleButton, 6f);
        SetSpeed(1f);
    }

    public void SetSpeed(float speed)
    {
        if (speed <= 0f && SimulationSpeed > 0f) speedBeforePause = SimulationSpeed;
        CurrentSpeed = SetSimulationSpeed(speed);
        if (CurrentSpeed > 0f) speedBeforePause = CurrentSpeed;
        RefreshSelection();
    }

    private void Update()
    {
        if (gameObject.scene.name != "World" || !Input.GetKeyDown(KeyCode.Space)) return;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && (selected.GetComponent<InputField>() != null || selected.GetComponent<TMPro.TMP_InputField>() != null)) return;
        if (!CanTogglePause()) return;
        // Space is also a UI Submit key. Clear focus before EventSystem handles it.
        EventSystem.current?.SetSelectedGameObject(null);
        TogglePause();
    }

    private bool CanTogglePause()
    {
        if (GameFoundation.MetaProgression.PortalTowerProgression.IsChoosingUpgrade ||
            GameFoundation.MetaProgression.FirstRunLightTutorial.BlocksInput) return false;
        if (Object.FindFirstObjectByType<GameFoundation.Settings.GameMenuController>() != null) return false;
        // A dialogue or another system owns this pause; Space must not release it.
        return SimulationSpeed > 0f || CurrentSpeed <= 0f;
    }

    public void TogglePause()
    {
        if (!CanTogglePause()) return;
        SetSpeed(SimulationSpeed > 0f ? 0f : speedBeforePause);
    }

    public static float SetSimulationSpeed(float speed)
    {
        if (GameFoundation.MetaProgression.PortalTowerProgression.IsChoosingUpgrade) speed = 0;
        SimulationSpeed = Mathf.Clamp(speed, 0f, 6f);
        Time.timeScale = SimulationSpeed;
        return SimulationSpeed;
    }

    private void Bind(Button button, float speed)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SetSpeed(speed));
    }

    private void RefreshSelection()
    {
        if (pauseButton != null && playSprite != null && pauseSprite != null)
            pauseButton.image.sprite = CurrentSpeed > 0f ? pauseSprite : playSprite;
        SetSelected(pauseSelected, Mathf.Approximately(CurrentSpeed, 0f));
        SetSelected(normalSelected, Mathf.Approximately(CurrentSpeed, 1f));
        SetSelected(doubleSelected, Mathf.Approximately(CurrentSpeed, 3f));
        SetSelected(quadrupleSelected, Mathf.Approximately(CurrentSpeed, 6f));
        Tint(normalButton,Mathf.Approximately(CurrentSpeed,1));
        Tint(doubleButton,Mathf.Approximately(CurrentSpeed,3));
        Tint(quadrupleButton,Mathf.Approximately(CurrentSpeed,6));
    }
    private void Tint(Button button,bool selected)
    { if(!button)return;var label=button.transform.Find("Pixel Label");if(label){var image=label.GetComponent<UnityEngine.UI.Image>();if(image)image.color=selected?activeLabelColor:inactiveLabelColor;} }

    private static void SetSelected(GameObject marker, bool selected)
    {
        if (marker != null) marker.SetActive(selected);
    }

    private void OnDestroy()
    {
        if (Time.timeScale == CurrentSpeed) Time.timeScale = 1f;
        SimulationSpeed = 1f;
    }
}
