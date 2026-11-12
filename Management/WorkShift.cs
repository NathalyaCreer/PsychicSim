
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Scene Names")]
    [SerializeField] private string storeSceneName = "Store";
    [SerializeField] private string apartmentSceneName = "Apartment";

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

            if (ReputationSystem.Instance != null)
                ReputationSystem.Instance.CompleteShift();

            SetPhase(ShiftPhase.Dusk);
        }
    }

    public void OpenStore()
    {
        if (CurrentPhase != ShiftPhase.Dawn)
        {
            Debug.LogWarning("The store can only be opened at Dawn.");
            return;
        }

        ShiftTimeRemaining = shiftDurationMinutes * 60f;
        shiftTimerRunning = true;

        SetPhase(ShiftPhase.Daytime);
    }

    public void GoUpstairs()
    {
        if (CurrentPhase != ShiftPhase.Dusk)
        {
            Debug.LogWarning("The player can only go upstairs after closing.");
            return;
        }

        SetPhase(ShiftPhase.Afterhours);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadApartment(apartmentSceneName);
        else
            Debug.LogError("SceneTransitionManager is missing.");
    }

    public void SleepAndStartNextDay()
    {
        if (CurrentPhase != ShiftPhase.Afterhours)
        {
            Debug.LogWarning("The player can only sleep during Afterhours.");
            return;
        }

        DayNumber++;
        ShiftTimeRemaining = 0f;
        shiftTimerRunning = false;

        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.BeginNewDay();

        SetPhase(ShiftPhase.Dawn);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadStore(storeSceneName);
        else
            Debug.LogError("SceneTransitionManager is missing.");
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
