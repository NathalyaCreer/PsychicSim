
using UnityEngine;
using System;

public class ReputationSystem : MonoBehaviour
{
    public static ReputationSystem Instance { get; private set; }

    public enum ShiftResult
    {
        Poor,
        Average,
        Good
    }
#region  Parameters and stuff
    [Header("Daily Reputation")]
    [SerializeField, Range(0, 100)] private int startingDailyReputation = 50;
    [SerializeField] private int minimumReputation = 0;
    [SerializeField] private int maximumReputation = 100;
    [SerializeField] private int failureThreshold = 5;

    [Header("Reputation Points")]
    [SerializeField] private int startingReputationPoints = 0;
    [SerializeField] private int reputationPerPoint = 50;

    [Header("Event Rewards")]
    [SerializeField] private int successfulReadingReward = 5;
    [SerializeField] private int failedReadingPenalty = 5;
    [SerializeField] private int highQualityProductReward = 3;
    [SerializeField] private int lowQualityProductPenalty = 3;
    [SerializeField] private int successfulExorcismReward = 10;
    [SerializeField] private int possessionPenalty = 10;
    [SerializeField] private int badGuyMischiefPenalty = 5;
    [SerializeField] private int customerDeathPenalty = 15;

    [Header("Customer Traffic - Based on Previous Shift")]
    [SerializeField] private int highReputationThreshold = 75;
    [SerializeField] private int lowReputationThreshold = 25;
    [SerializeField] private int highReputationBonusCustomers = 2;
    [SerializeField] private int lowReputationCustomerReduction = 2;

    [Header("Persistence")]
    [SerializeField] private bool loadSavedData = true;

    private const string DailyReputationKey = "DailyReputation";
    private const string PreviousShiftReputationKey = "PreviousShiftReputation";
    private const string ReputationPointsKey = "ReputationPoints";
    private const string ConversionProgressKey = "ReputationConversionProgress";

    public int DailyReputation { get; private set; }
    public int PreviousShiftReputation { get; private set; }
    public int ReputationPoints { get; private set; }
    public int ConversionProgress { get; private set; }

    public bool HasFailed => DailyReputation < failureThreshold;

    public event Action<int> OnDailyReputationChanged;
    public event Action<int> OnPreviousShiftReputationChanged;
    public event Action<int> OnReputationPointsChanged;
    public event Action OnGameOver;

    private bool gameOverTriggered;
    private bool shiftFinalized;
#endregion
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple ReputationSystem instances found.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (loadSavedData)
        {
            DailyReputation = PlayerPrefs.GetInt(
                DailyReputationKey,
                startingDailyReputation
            );

            PreviousShiftReputation = PlayerPrefs.GetInt(
                PreviousShiftReputationKey,
                startingDailyReputation
            );

            ReputationPoints = PlayerPrefs.GetInt(
                ReputationPointsKey,
                startingReputationPoints
            );

            ConversionProgress = PlayerPrefs.GetInt(
                ConversionProgressKey,
                0
            );
        }
        else
        {
            DailyReputation = startingDailyReputation;
            PreviousShiftReputation = startingDailyReputation;
            ReputationPoints = startingReputationPoints;
            ConversionProgress = 0;
        }

        DailyReputation = Mathf.Clamp(
            DailyReputation,
            minimumReputation,
            maximumReputation
        );

        PreviousShiftReputation = Mathf.Clamp(
            PreviousShiftReputation,
            minimumReputation,
            maximumReputation
        );

        SaveData();
    }

    /// <summary>
    /// Records a reputation change during the current shift.
    /// </summary>
    public void AddReputation(int amount, string reason)
    {
        if (gameOverTriggered || amount == 0)
            return;

        int previousValue = DailyReputation;

        DailyReputation = Mathf.Clamp(
            DailyReputation + amount,
            minimumReputation,
            maximumReputation
        );

        int actualChange = DailyReputation - previousValue;

        // Only real positive gains count toward reputation points.
        if (actualChange > 0)
        {
            ConversionProgress += actualChange;

            while (reputationPerPoint > 0 &&
                   ConversionProgress >= reputationPerPoint)
            {
                ConversionProgress -= reputationPerPoint;
                ReputationPoints++;

                OnReputationPointsChanged?.Invoke(ReputationPoints);
            }
        }

        SaveData();

        Debug.Log(
            $"Daily reputation: {DailyReputation}/100 " +
            $"({actualChange:+#;-#;0}). Reason: {reason}. " +
            $"Reputation points: {ReputationPoints}"
        );

        if (actualChange != 0)
            OnDailyReputationChanged?.Invoke(DailyReputation);

        CheckFailure();
    }

    /// <summary>
    /// Call once when the store shift ends.
    /// Saves today's score as the score that affects the next shift.
    /// </summary>
    public void CompleteShift()
    {
        if (shiftFinalized)
            return;

        shiftFinalized = true;
        PreviousShiftReputation = DailyReputation;

        SaveData();

        OnPreviousShiftReputationChanged?.Invoke(
            PreviousShiftReputation
        );

        Debug.Log(
            $"Shift completed. Final reputation: " +
            $"{PreviousShiftReputation}/100. This score affects next shift."
        );
    }

    /// <summary>
    /// Call when the player sleeps and the next day begins.
    /// </summary>
    public void BeginNewDay()
    {
        DailyReputation = Mathf.Clamp(
            startingDailyReputation,
            minimumReputation,
            maximumReputation
        );

        shiftFinalized = false;
        gameOverTriggered = false;

        SaveData();

        OnDailyReputationChanged?.Invoke(DailyReputation);

        Debug.Log(
            $"New day started. Daily reputation reset to {DailyReputation}/100. " +
            $"Previous shift score remains {PreviousShiftReputation}/100."
        );
    }
#region Report Cards
    public void ReportCardReading(bool successful)
    {
        if (successful)
            AddReputation(successfulReadingReward, "Successful card reading");
        else
            AddReputation(-failedReadingPenalty, "Failed card reading");
    }

    public void ReportProductQuality(bool highQuality)
    {
        if (highQuality)
            AddReputation(highQualityProductReward, "High-quality product sold");
        else
            AddReputation(-lowQualityProductPenalty, "Low-quality product sold");
    }

    public void ReportCleanliness(bool clean)
    {
        AddReputation(clean ? 1 : -1,
            clean ? "Store cleanliness maintained" : "Store is dirty");
    }

    public void ReportExorcism(bool successful)
    {
        if (successful)
            AddReputation(successfulExorcismReward, "Successful exorcism");
    }

    public void ReportPossession(bool successful)
    {
        if (successful)
            AddReputation(-possessionPenalty, "Customer successfully possessed");
    }

    public void ReportBadGuyMischief(bool successful)
    {
        if (successful)
            AddReputation(-badGuyMischiefPenalty, "Bad-guy mischief");
    }

    public void ReportCustomerDeath()
    {
        AddReputation(-customerDeathPenalty, "Customer killed");
    }

    public void ReportEmployeeDeath()
    {
        AddReputation(-customerDeathPenalty, "Employee killed");
    }
#endregion


    // These traffic methods now use the LAST COMPLETED shift's score.
    public int GetMaxCustomers(int baseMaxCustomers)
    {
        switch (GetPreviousShiftResult())
        {
            case ShiftResult.Poor:
                return Mathf.Max(1, baseMaxCustomers - 2);

            case ShiftResult.Good:
                return baseMaxCustomers + 2;

            default:
                return baseMaxCustomers;
        }
    }

    public float GetSpawnInterval(float baseSpawnInterval)
    {
        switch (GetPreviousShiftResult())
        {
            case ShiftResult.Poor:
                return baseSpawnInterval * 1.5f;

            case ShiftResult.Good:
                return baseSpawnInterval * 0.75f;

            default:
                return baseSpawnInterval;
        }
    }

    private void CheckFailure()
    {
        if (DailyReputation >= failureThreshold || gameOverTriggered)
            return;

        gameOverTriggered = true;

        Debug.LogError(
            "GAME OVER: Daily reputation fell below the failure threshold."
        );

        SaveData();
        OnGameOver?.Invoke();
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt(DailyReputationKey, DailyReputation);
        PlayerPrefs.SetInt(PreviousShiftReputationKey, PreviousShiftReputation);
        PlayerPrefs.SetInt(ReputationPointsKey, ReputationPoints);
        PlayerPrefs.SetInt(ConversionProgressKey, ConversionProgress);
        PlayerPrefs.Save();
    }

    public void ResetReputation()
    {
        DailyReputation = startingDailyReputation;
        PreviousShiftReputation = startingDailyReputation;
        ReputationPoints = startingReputationPoints;
        ConversionProgress = 0;

        shiftFinalized = false;
        gameOverTriggered = false;

        SaveData();

        OnDailyReputationChanged?.Invoke(DailyReputation);
        OnPreviousShiftReputationChanged?.Invoke(PreviousShiftReputation);
        OnReputationPointsChanged?.Invoke(ReputationPoints);
    }

    public ShiftResult GetPreviousShiftResult()
    {
        if (PreviousShiftReputation <= 40)
            return ShiftResult.Poor;
        
        if (PreviousShiftReputation <= 60)
            return ShiftResult.Average;
        
        return ShiftResult.Good;
    }

    public bool TrySpendReputationPoints(int amount)
    {
        if (amount <= 0 || ReputationPoints < amount)
            return false;

        ReputationPoints -= amount;

        SaveData();
        OnReputationPointsChanged?.Invoke(ReputationPoints);

        Debug.Log($"Spent {amount} reputation point(s).");
        return true;
    }
}
