
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    [Header("Spawning")]
    [SerializeField] private Customer customerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int maxActiveCustomers = 5;
    [SerializeField] private Transform customerEntrancePoint;

    [Header("Work Shift")]
    [SerializeField] private WorkShift workShift;

    [Header("Queue")]
    [SerializeField] private QueueLine checkoutQueue;
    [SerializeField] private CheckoutUI sceneCheckoutUI;

    [Header("NavMesh")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Customer Systems")]
    [SerializeField] private ReadingCards sceneReadingCards;
    [SerializeField] private FortuneTellingLine fortuneTellingLine;

    [Header("Checkout Setup")]
    [SerializeField] private BagSpawn sceneBagSpawn;
    [SerializeField] private Transform[] checkoutItemPositions;
    [SerializeField] private Transform storeExit;

    [Header("Reputation")]
    [SerializeField] private ReputationSystem reputationSystem;

    private readonly List<ReStockShelf> registeredShelves =
        new List<ReStockShelf>();

    private readonly List<Customer> activeCustomers =
        new List<Customer>();

    private Coroutine spawnRoutine;

    public bool HasCustomersRemaining => ActiveCustomerCount > 0;

    public int ActiveCustomerCount
    {
        get
        {
            activeCustomers.RemoveAll(customer => customer == null);
            return activeCustomers.Count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        registeredShelves.Clear();
    }

    private void OnEnable()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnCustomersRoutine());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator SpawnCustomersRoutine()
    {
        while (workShift == null)
        {
            workShift = WorkShift.Instance;
            yield return null;
        }

        while (true)
        {
            activeCustomers.RemoveAll(customer => customer == null);

            if (workShift.CurrentPhase == WorkShift.ShiftPhase.Daytime)
            {
                int currentMaxCustomers = reputationSystem != null
                    ? reputationSystem.GetMaxCustomers(maxActiveCustomers)
                    : maxActiveCustomers;

                if (activeCustomers.Count < currentMaxCustomers)
                    SpawnCustomer();
            }

            yield return new WaitForSeconds(GetCurrentSpawnInterval());
        }
    }

    private float GetCurrentSpawnInterval()
    {
        float interval = reputationSystem != null
            ? reputationSystem.GetSpawnInterval(spawnInterval)
            : spawnInterval;

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

        Customer customer = Instantiate(
            customerPrefab,
            hit.position,
            spawnPoint.rotation
        );

        if (customer == null)
        {
            Debug.LogWarning(
                "CustomerSpawner: Spawned customer is missing its Customer component."
            );
            return;
        }

        customer.SetEntrancePoint(customerEntrancePoint);

        // Randomize customer appearance.
        CustomerAppearanceRandomizer appearance =
            customer.GetComponent<CustomerAppearanceRandomizer>();

        if (appearance != null)
            appearance.RandomizeAppearance();

        // Assign available shopping shelves.
        customer.SetShoppingShelves(GetShoppingShelves());

        // Assign queue and checkout references.
        customer.SetReadingCards(sceneReadingCards);
        customer.SetCheckoutQueue(checkoutQueue);
        customer.SetCheckoutUI(sceneCheckoutUI);
        customer.SetReputationSystem(reputationSystem);
        customer.SetExitPoint(storeExit);

        customer.InitializeCheckout(
            checkoutItemPositions,
            sceneBagSpawn
        );

        customer.SetFortuneTellingLine(fortuneTellingLine);

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

        List<Customer> eligible = new List<Customer>();

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
    }

    public void RegisterShelf(ReStockShelf shelf)
    {
        if (shelf == null)
            return;

        if (!registeredShelves.Contains(shelf))
        {
            registeredShelves.Add(shelf);

            Debug.Log(
                $"CustomerSpawner: Registered shelf {shelf.name}. " +
                $"Total shelves: {registeredShelves.Count}"
            );
        }
    }

    public void UnregisterShelf(ReStockShelf shelf)
    {
        if (shelf == null)
            return;

        if (registeredShelves.Remove(shelf))
        {
            Debug.Log(
                $"CustomerSpawner: Unregistered shelf {shelf.name}. " +
                $"Total shelves: {registeredShelves.Count}"
            );
        }
    }

    private ReStockShelf[] GetShoppingShelves()
    {
        registeredShelves.RemoveAll(shelf => shelf == null);
        return registeredShelves.ToArray();
    }
}
