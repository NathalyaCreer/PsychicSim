
using System.Collections;
using UnityEngine;

public class PossessionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorkShift workShift;
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private ReputationSystem reputationSystem;

    [Header("Possession Settings")]
    [SerializeField, Range(0f, 1f)]
    private float possessionChance = 0.5f;

    [SerializeField, Min(0f)]
    private float delayBeforeFirstAttempt = 0f;

    private Coroutine possessionRoutine;
    private bool shiftActive;

    private void Awake()
    {
        if (reputationSystem == null)
            reputationSystem = ReputationSystem.Instance;
    }

    private void OnEnable()
    {
        if (workShift != null)
            workShift.PhaseChanged += OnPhaseChanged;
    }

    private void Start()
    {
        if (workShift == null || customerSpawner == null)
        {
            Debug.LogError(
                "PossessionManager needs a WorkShift and CustomerSpawner.",
                this
            );
            enabled = false;
            return;
        }

        if (workShift.CurrentPhase == WorkShift.ShiftPhase.Daytime)
            StartPossessionSchedule();
    }

    private void OnDisable()
    {
        if (workShift != null)
            workShift.PhaseChanged -= OnPhaseChanged;

        StopPossessionSchedule();
    }

    private void OnPhaseChanged(WorkShift.ShiftPhase phase)
    {
        if (phase == WorkShift.ShiftPhase.Daytime)
        {
            StartPossessionSchedule();
        }
        else
        {
            StopPossessionSchedule();
        }
    }

    private void StartPossessionSchedule()
    {
        StopPossessionSchedule();

        shiftActive = true;
        possessionRoutine = StartCoroutine(PossessionSchedule());
    }

    private void StopPossessionSchedule()
    {
        shiftActive = false;

        if (possessionRoutine != null)
        {
            StopCoroutine(possessionRoutine);
            possessionRoutine = null;
        }
    }

    private IEnumerator PossessionSchedule()
    {
        float shiftDuration = workShift.ShiftTimeRemaining;

        if (shiftDuration <= 0f)
            yield break;

        if (delayBeforeFirstAttempt > 0f)
            yield return new WaitForSeconds(delayBeforeFirstAttempt);

        // Four attempts, one at the beginning of each quarter.
        for (int i = 0; i < 4; i++)
        {
            if (!shiftActive ||
                workShift.CurrentPhase != WorkShift.ShiftPhase.Daytime)
                yield break;

            if (i > 0)
            {
                float quarterDuration = shiftDuration / 4f;
                yield return new WaitForSeconds(quarterDuration);
            }

            if (!shiftActive ||
                workShift.CurrentPhase != WorkShift.ShiftPhase.Daytime)
                yield break;

            AttemptPossession();
        }

        possessionRoutine = null;
    }

    private void AttemptPossession()
    {
        if (customerSpawner.HasPossessedCustomer)
        {
            Debug.Log("Possession attempt skipped: a possessed customer is already active.");
            return;
        }

        if (Random.value > possessionChance)
        {
            Debug.Log("The possession attempt failed.");
            return;
        }

        Customer customer = customerSpawner.GetRandomEligibleCustomer();

        if (customer == null)
        {
            Debug.Log("Possession attempt skipped: no eligible customer is available.");
            return;
        }

        PossessedCustomer possessed =
            customer.GetComponent<PossessedCustomer>();

        if (possessed == null)
        {
            Debug.LogError(
                $"{customer.name} has no PossessedCustomer component. " +
                "Add it to the customer prefab.",
                customer
            );
            return;
        }

        PossessedCustomer.PossessionBehavior behavior =
            Random.value < 0.5f
                ? PossessedCustomer.PossessionBehavior.Kill
                : PossessedCustomer.PossessionBehavior.Destroy;

        if (possessed.BeginPossession(behavior))
        {
            if (reputationSystem == null)
                reputationSystem = ReputationSystem.Instance;

            // Records the existing configurable possession penalty.
            if (reputationSystem != null)
                reputationSystem.ReportPossession(true);

            customerSpawner.RegisterPossessedCustomer(possessed);

            Debug.Log(
                $"{customer.name} has become possessed. Behavior: {behavior}",
                customer
            );
        }
    }
}
