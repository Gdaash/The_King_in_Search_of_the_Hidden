using UnityEngine;
using UnityEngine.UI;

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

    public float CurrentSpeed { get; private set; } = 1f;

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() => SetSpeed(CurrentSpeed > 0f ? 0f : 1f));
        }
        Bind(normalButton, 1f);
        Bind(doubleButton, 2f);
        Bind(quadrupleButton, 3f);
        SetSpeed(1f);
    }

    public void SetSpeed(float speed)
    {
        CurrentSpeed = SetSimulationSpeed(speed);
        RefreshSelection();
    }

    public static float SetSimulationSpeed(float speed)
    {
        if (GameFoundation.MetaProgression.PortalTowerProgression.IsChoosingUpgrade) speed = 0;
        SimulationSpeed = Mathf.Clamp(speed, 0f, 4f);
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
        SetSelected(doubleSelected, Mathf.Approximately(CurrentSpeed, 2f));
        SetSelected(quadrupleSelected, Mathf.Approximately(CurrentSpeed, 3f));
    }

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
