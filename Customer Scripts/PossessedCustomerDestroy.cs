
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PossessedCustomerDestroy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PossessedCustomer possessedCustomer;
    [SerializeField] private GameObject fireHazardPrefab;

    [Header("Furniture Targets")]
    [SerializeField] private FurnitureFireTarget currentFurnitureTarget;
    [SerializeField] private float targetReachDistance = 1.5f;

    [Header("Fire Preparation")]
    [SerializeField] private float firePreparationTime = 5f;
    [SerializeField] private Vector3 fireSpawnOffset = Vector3.zero;

    [Header("Sabotage")]
    [SerializeField] private bool continueToNextFurniture = true;
    [SerializeField] private float delayBetweenFires = 2f;


    public bool IsDestroyModeActive { get; private set; }
    public bool IsPreparingFire { get; private set; }
    public bool WasInterrupted { get; private set; }

    private Coroutine sabotageCoroutine;
    private readonly HashSet<Transform> sabotagedFurniture =
        new HashSet<Transform>();

    private Transform currentTarget;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (possessedCustomer == null)
            possessedCustomer = GetComponent<PossessedCustomer>();
    }

    /// <summary>
    /// Call this when the customer successfully enters Destroy possession mode.
    /// </summary>
    public void BeginDestroyMode()
    {
        if (IsDestroyModeActive)
            return;

        IsDestroyModeActive = true;
        WasInterrupted = false;

        if (sabotageCoroutine != null)
            StopCoroutine(sabotageCoroutine);

        sabotageCoroutine = StartCoroutine(DestroyModeRoutine());
    }

    private IEnumerator DestroyModeRoutine()
    {
        while (IsDestroyModeActive && !WasInterrupted)
        {
            currentFurnitureTarget = FindNextFurnitureTarget();

            if (currentFurnitureTarget == null)
            {
                Debug.Log($"{name} has no available furniture targets.", this);
                IsDestroyModeActive = false;
                yield break;
            }

            // Move to the selected furniture.
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(currentFurnitureTarget.Position);

                while (!WasInterrupted &&
                       agent.enabled &&
                       agent.isOnNavMesh &&
                       (agent.pathPending ||
                        agent.remainingDistance > targetReachDistance))
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning(
                    $"{name} cannot navigate because its NavMeshAgent is unavailable.",
                    this);

                IsDestroyModeActive = false;
                yield break;
            }

            if (WasInterrupted)
                yield break;

            // Stop moving while preparing the fire.
            agent.isStopped = true;
            IsPreparingFire = true;

            float preparationTimer = 0f;

            while (preparationTimer < firePreparationTime &&
                   !WasInterrupted)
            {
                preparationTimer += Time.deltaTime;
                yield return null;
            }

            IsPreparingFire = false;

            if (WasInterrupted)
                yield break;

            // Preparation completed: start the fire.
            StartFireAtFurniture(currentFurnitureTarget);
            currentFurnitureTarget.MarkBurned();

            sabotagedFurniture.Add(currentTarget);

            if (!continueToNextFurniture)
            {
                IsDestroyModeActive = false;
                yield break;
            }

            agent.isStopped = false;
            yield return new WaitForSeconds(delayBetweenFires);
        }

        IsPreparingFire = false;
        IsDestroyModeActive = false;
    }

    private FurnitureFireTarget FindNextFurnitureTarget()
    {
        if (FurnitureFireTargetManager.Instance == null)
        {
            Debug.LogWarning(
                "No FurnitureFireTargetManager exists in the scene.",
                this);

            return null;
        }

        return FurnitureFireTargetManager.Instance.GetRandomAvailableTarget();
    }

    private void StartFireAtFurniture(FurnitureFireTarget furniture)
    {
        if (furniture == null || fireHazardPrefab == null)
            return;

        Instantiate(
            fireHazardPrefab,
            furniture.FirePosition + fireSpawnOffset,
            Quaternion.identity);

        Debug.Log($"{name} started a fire at {furniture.name}.", this);
    }

    /// <summary>
    /// Call this when the player successfully pushes the possessed customer.
    /// The existing push/throw system should handle the physical throw.
    /// </summary>
    public void InterruptSabotage()
    {
        if (!IsDestroyModeActive)
            return;

        WasInterrupted = true;
        IsPreparingFire = false;
        IsDestroyModeActive = false;

        if (sabotageCoroutine != null)
        {
            StopCoroutine(sabotageCoroutine);
            sabotageCoroutine = null;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        Debug.Log($"{name}'s fire sabotage was interrupted.", this);
    }

    public void ResumeDestroyMode()
    {
        if (WasInterrupted)
            return;

        BeginDestroyMode();
    }
}

