
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

    [Header("Shift Settings")]
    [SerializeField] private float shiftDurationMinutes = 10f;

    public ShiftPhase CurrentPhase { get; private set; } = ShiftPhase.Dawn;
    public int DayNumber { get; private set; } = 1;

    public float ShiftTimeRemaining { get; private set; }

    // Other scripts can subscribe to this to react to phase changes.
    public event Action<ShiftPhase> PhaseChanged;

    private bool shiftTimerRunning;

    private void Start()
    {
        SetPhase(ShiftPhase.Dawn);
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
            {
                ReputationSystem.Instance.CompleteShift();
            }
            
            // Store closes automatically when the shift timer ends.
            SetPhase(ShiftPhase.Dusk);
        }
    }

    /// <summary>
    /// Call this when the player flips the store sign to OPEN.
    /// </summary>
    public void OpenStore()
    {
        if (CurrentPhase != ShiftPhase.Dawn)
        {
            Debug.LogWarning("The store can only be opened at Dawn.", this);
            return;
        }

        ShiftTimeRemaining = shiftDurationMinutes * 60f;
        shiftTimerRunning = true;

        SetPhase(ShiftPhase.Daytime);
    }

    /// <summary>
    /// Call this when the player confirms they are going upstairs.
    /// </summary>
    public void GoUpstairs()
    {
        if (CurrentPhase != ShiftPhase.Dusk)
        {
            Debug.LogWarning("The player can only go upstairs after closing.", this);
            return;
        }

        SetPhase(ShiftPhase.Afterhours);
    }

    /// <summary>
    /// Call this when the player chooses to sleep.
    /// Starts the next day at Dawn.
    /// </summary>
    public void SleepAndStartNextDay()
    {
        if (CurrentPhase != ShiftPhase.Afterhours)
        {
            Debug.LogWarning(
                "The player can only sleep during Afterhours.",
                this
            );
            return;
        }

        DayNumber++;
        ShiftTimeRemaining = 0f;
        shiftTimerRunning = false;

        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.BeginNewDay();

        SetPhase(ShiftPhase.Dawn);
    }

    private void SetPhase(ShiftPhase newPhase)
    {
        if (CurrentPhase == newPhase)
            return;

        CurrentPhase = newPhase;

        Debug.Log($"Day {DayNumber}: phase changed to {CurrentPhase}.", this);

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

/*
note: before player starts shift the time is {Dawn}
1. at Dawn packages are delivered to back door (packages can only be ordered during Afterhours)
2. backdoor is only accessible during Dawn.


each shift is 12 hours (about 10 mins) {Daytime}
1.Player flips sign to open store (starting shift timer)
2.Customers spawn within this time in relation to the previous shift's reputation score (ie player had 40/100 reputation by the end of the shift so current shift will have a 2% decrease in customers)
3. Backdoor is now locked and cannot be entered.
4.Player is in Store Scene

Closing Shift (after store closes) {Dusk}
1. Freetime for player to roam and decorate and whatever before heading upstairs.
2. The upstairs apartment is unlocked using an activated trigger collider that gives the player a confirmation UI in ending the day

Afterhours (once player heads upstairs) {Night}
1. a new scene loaded into the upstairs apartment
2. Player can access laptop (supply store, reputation points store)
3. Player can decorate apartment
4. Small minigames within apartment (cooking minigame where finished meals appear in breakroom next shift)
5. Once player sleeps, the next day loads and they are spawned into the store scene {Dawn} 
*/
