
using System;
using UnityEngine;

public class WorkShift : MonoBehaviour
{
    public enum ShiftPhase
    {
        Dawn,
        Daytime,
        Dusk,
        Afterhours
    }

    public static WorkShift Instance { get; private set; }

    [Header("Shift Settings")]
    [SerializeField] private float shiftDurationMinutes = 10f;

    public ShiftPhase CurrentPhase { get; private set; } = ShiftPhase.Dawn;
    public int DayNumber { get; private set; } = 1;
    public float ShiftTimeRemaining { get; private set; }

    public float ShiftProgress01 =>
        shiftDurationMinutes <= 0f ? 1f :
        1f - Mathf.Clamp01(
            ShiftTimeRemaining / (shiftDurationMinutes * 60f)
        );

    public event Action<ShiftPhase> PhaseChanged;

    private bool shiftTimerRunning;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!shiftTimerRunning)
            return;

        ShiftTimeRemaining -= Time.deltaTime;

        if (ShiftTimeRemaining <= 0f)
        {
            ShiftTimeRemaining = 0f;
            shiftTimerRunning = false;

            EmployeeManager.Instance?.PayEmployeesForDay(DayNumber);

            if (ReputationSystem.Instance != null)
                ReputationSystem.Instance.CompleteShift();

            SetPhase(ShiftPhase.Dusk);
        }
    }

    public void OpenStore()
    {
        if (CurrentPhase != ShiftPhase.Dawn)
            return;

        ShiftTimeRemaining = shiftDurationMinutes * 60f;
        shiftTimerRunning = true;

        SetPhase(ShiftPhase.Daytime);
    }

    public void GoUpstairs()
    {
        if (CurrentPhase != ShiftPhase.Dusk || isTransitioning)
            return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("WorkShift: SceneTransitionManager is missing.");
            return;
        }

        isTransitioning = true;

        SceneTransitionManager.Instance.LoadApartment();

        SetPhase(ShiftPhase.Afterhours);

        isTransitioning = false;
    }

    public void SleepAndStartNextDay()
    {
        if (CurrentPhase != ShiftPhase.Afterhours || isTransitioning)
            return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("WorkShift: SceneTransitionManager is missing.");
            return;
        }

        isTransitioning = true;

        DayNumber++;

        ReputationSystem.Instance?.BeginNewDay();

        ShiftTimeRemaining = 0f;
        shiftTimerRunning = false;

        EmployeeManager.Instance?.GenerateDailyApplicants();

        SetPhase(ShiftPhase.Dawn);

        SceneTransitionManager.Instance.LoadStore();

        isTransitioning = false;
    }

    private void SetPhase(ShiftPhase newPhase)
    {
        if (CurrentPhase == newPhase)
            return;

        CurrentPhase = newPhase;

        Debug.Log($"Day {DayNumber}: phase changed to {CurrentPhase}.");

        PhaseChanged?.Invoke(CurrentPhase);
    }

    public string GetShiftTimeDisplay()
    {
        int totalSeconds = Mathf.CeilToInt(ShiftTimeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}
