
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Customer customerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int maxActiveCustomers = 5;

    [Header("Work Shift")]
    [SerializeField] private WorkShift workShift;

    [Header("Queue")]
    [SerializeField] private QueueLine checkoutQueue;
    [SerializeField] private CheckoutUI sceneCheckoutUI;

    [Header("NavMesh")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Shopping")]
    [SerializeField] private Transform[] shoppingStops;

    [Header("Customer Systems")]
    [SerializeField] private ReadingCards sceneReadingCards;
    [SerializeField] private FortuneTellingLine fortuneTellingLine;

    [Header("Checkout Setup")]
    [SerializeField] private BagSpawn sceneBagSpawn;
    [SerializeField] private Transform[] checkoutItemPositions;
    [SerializeField] private Transform storeExit;

    [Header("Reputation")]
    [SerializeField] private ReputationSystem reputationSystem;

    private readonly List<Customer> activeCustomers =
        new List<Customer>();

    private void Start()
    {
        if (workShift == null)
        {
            Debug.LogError(
                "CustomerSpawner: Assign the WorkShift reference.",
                this
            );
            return;
        }

        StartCoroutine(SpawnCustomersRoutine());
    }

    private IEnumerator SpawnCustomersRoutine()
    {
        while (true)
        {
            // Remove customers that have been destroyed.
            activeCustomers.RemoveAll(customer => customer == null);

            // Only spawn new customers while the store is open.
            if (workShift.CurrentPhase == WorkShift.ShiftPhase.Daytime)
            {
                int currentMaxCustomers = reputationSystem != null
                    ? reputationSystem.GetMaxCustomers(maxActiveCustomers)
                    : maxActiveCustomers;

                if (activeCustomers.Count < currentMaxCustomers)
                    SpawnCustomer();
            }

            // Recheck the phase and customer count after each interval.
            yield return new WaitForSeconds(GetCurrentSpawnInterval());
        }
    }

    private float GetCurrentSpawnInterval()
    {
        float interval = reputationSystem != null
            ? reputationSystem.GetSpawnInterval(spawnInterval)
            : spawnInterval;

        // Prevent zero or negative wait intervals.
        return Mathf.Max(0.1f, interval);
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError(
                "CustomerSpawner: Assign the customer prefab and spawn point.",
                this
            );
            return;
        }

        // Find a nearby valid NavMesh position.
        if (!NavMesh.SamplePosition(
                spawnPoint.position,
                out NavMeshHit hit,
                navMeshSampleDistance,
                NavMesh.AllAreas))
        {
            Debug.LogWarning(
                "CustomerSpawner: No NavMesh found near the spawn point.",
                this
            );
            return;
        }

        // Spawn the customer.
        Customer customer = Instantiate(
            customerPrefab,
            hit.position,
            spawnPoint.rotation
        );


        // Assign shopping and queue references.
        customer.SetShoppingStops(shoppingStops);
        customer.SetReadingCards(sceneReadingCards);
        customer.SetCheckoutQueue(checkoutQueue);
        customer.SetCheckoutUI(sceneCheckoutUI);
        customer.SetReputationSystem(reputationSystem);
        customer.SetExitPoint(storeExit);

        // Assign checkout bag spawner and counter positions.
        customer.InitializeCheckout(
            checkoutItemPositions,
            sceneBagSpawn
        );

        // Assign additional customer systems.
        customer.SetFortuneTellingLine(fortuneTellingLine);

        // Track this customer.
        activeCustomers.Add(customer);

        Debug.Log(
            $"Spawned {customer.name}. Active customers: {activeCustomers.Count}"
        );
    }

    public bool HasPossessedCustomer
    {
        get
        {
            activeCustomers.RemoveAll(customer => customer == null);

            foreach (Customer customer in activeCustomers)
            {
                if (customer == null)
                    continue;

                PossessedCustomer possessed =
                    customer.GetComponent<PossessedCustomer>();

                if (possessed != null && possessed.IsPossessed)
                    return true;
            }

            return false;
        }
    }

    public Customer GetRandomEligibleCustomer()
    {
        activeCustomers.RemoveAll(customer => customer == null);

        System.Collections.Generic.List<Customer> eligible =
            new System.Collections.Generic.List<Customer>();

        foreach (Customer customer in activeCustomers)
        {
            if (customer == null)
                continue;

            PossessedCustomer possessed =
                customer.GetComponent<PossessedCustomer>();

            if (possessed == null ||
                possessed.CurrentState == PossessedCustomer.PossessionState.Normal)
            {
                eligible.Add(customer);
            }
        }

        if (eligible.Count == 0)
            return null;

        return eligible[Random.Range(0, eligible.Count)];
    }

    public void RegisterPossessedCustomer(PossessedCustomer customer)
    {
        // The spawned customer is already tracked in activeCustomers.
        // This method is available for future possession tracking and events.
    }
}
