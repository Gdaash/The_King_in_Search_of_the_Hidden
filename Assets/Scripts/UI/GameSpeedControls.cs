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

    public float CurrentSpeed { get; private set; } = 1f;

    private void Awake()
    {
        Bind(pauseButton, 0f);
        Bind(normalButton, 1f);
        Bind(doubleButton, 2f);
        Bind(quadrupleButton, 4f);
        SetSpeed(1f);
    }

    public void SetSpeed(float speed)
    {
        CurrentSpeed = SetSimulationSpeed(speed);
        RefreshSelection();
    }

    public static float SetSimulationSpeed(float speed)
    {
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
        SetSelected(pauseSelected, Mathf.Approximately(CurrentSpeed, 0f));
        SetSelected(normalSelected, Mathf.Approximately(CurrentSpeed, 1f));
        SetSelected(doubleSelected, Mathf.Approximately(CurrentSpeed, 2f));
        SetSelected(quadrupleSelected, Mathf.Approximately(CurrentSpeed, 4f));
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
