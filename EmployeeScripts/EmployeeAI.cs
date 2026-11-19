
using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EmployeeAI : MonoBehaviour
{
    [Header("Employee Information")]
    public EmployeeApplicant EmployeeData { get; private set; }

    public string EmployeeName =>
        EmployeeData != null ? EmployeeData.employeeName : "Employee";

    public int Fortitude =>
        EmployeeData != null ? EmployeeData.fortitude : 0;

    public int WorkEthic =>
        EmployeeData != null ? EmployeeData.workEthic : 0;

    public int Speed =>
        EmployeeData != null ? EmployeeData.speed : 0;

    [Header("Movement")]
    [SerializeField] private float minimumSpeed = 2f;
    [SerializeField] private float maximumSpeed = 6f;

    [Header("Cleanup")]
    [SerializeField] private bool canCleanMess = true;
    [SerializeField] private float messSearchInterval = 2f;
    [SerializeField] private float cleaningDuration = 1.5f;
    [SerializeField] private float messSearchRadius = 30f;

    [Header("Cleanup Sounds")]
    [SerializeField] private AudioClip trashPickupSound;
    [SerializeField] private AudioClip spillCleaningSound;
    [SerializeField, Range(0f, 1f)] private float cleaningVolume = 1f;

    [Header("Cashier Setup")]
    [SerializeField] private Transform cashierStation;

    [Header("Furniture Repair")]
    [SerializeField] private bool canRepairFurniture = true;
    [SerializeField] private float furnitureSearchInterval = 2f;
    [SerializeField] private float furnitureSearchRadius = 30f;

    [Tooltip("Repair duration for employees with maximum Speed.")]
    [SerializeField] private float fastestRepairDuration = 2f;

    [Tooltip("Repair duration for employees with minimum Speed.")]
    [SerializeField] private float slowestRepairDuration = 8f;

    private QueueLine checkoutQueue;
    private CheckoutUI cashierCheckoutUI;
    private BagSpawn cashierBagSpawn;

    private Customer cashierCustomer;

    private bool cashierAtStation;
    private bool cashierCheckoutAttempted;
    private bool cashierConfigured;

    private NavMeshAgent agent;
    private AudioSource audioSource;

    private Action onShiftEnded;
    private bool leavingStore;

    private StoreMess currentMess;
    private float searchTimer;
    private float cleaningTimer;
    private bool isCleaning;
    private bool navMeshWarningShown;

    private bool shiftEndRequested;
    private Transform pendingExitPoint;
    private Action pendingShiftCallback;

    private NavMeshPath navigationPath;

    public bool IsWorking => !leavingStore;
    private FixableFurniture currentFurniture;
    private float furnitureSearchTimer;
    private float furnitureRepairTimer;
    private float furnitureRepairDuration;
    private bool isRepairingFurniture;

    // Prevents multiple maintenance employees from repairing the same furniture simultaneously.
    private static readonly System.Collections.Generic.HashSet<FixableFurniture>
        claimedFurniture = new();

    public EmployeeRole Role =>
        EmployeeData != null
            ? EmployeeData.role
            : EmployeeRole.Maintenance;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        navigationPath = new NavMeshPath();
    }

    public void Initialize(EmployeeApplicant employee)
    {
        if (employee == null)
        {
            Debug.LogError("EmployeeAI received invalid employee data.");
            return;
        }

        EmployeeData = employee;

        float speedMultiplier = Mathf.Lerp(
            minimumSpeed,
            maximumSpeed,
            Speed / 100f
        );

        agent.speed = speedMultiplier;

        gameObject.name = $"Employee - {EmployeeName}";

        Debug.Log(
            $"{EmployeeName} arrived. " +
            $"Fortitude: {Fortitude}, " +
            $"Work Ethic: {WorkEthic}, " +
            $"Speed: {Speed}"
        );
    }

    private void Update()
    {
        if (leavingStore)
        {
            UpdateLeaving();
            return;
        }

        if (agent == null || !agent.enabled)
            return;

        if (!agent.isOnNavMesh)
        {
            if (Role == EmployeeRole.Cashier && !navMeshWarningShown)
            {
                navMeshWarningShown = true;

                Debug.LogError(
                    $"{EmployeeName}: Cashier NavMeshAgent is no longer on the NavMesh!",
                    this
                );
            }

            return;
        }

        switch (Role)
        {
            case EmployeeRole.Cashier:
                UpdateCashier();
                break;

            case EmployeeRole.Maintenance:
                UpdateMaintenance();
                break;
        }
    }

    // --------------------------------------------------
    // CLEANUP BEHAVIOR
    // --------------------------------------------------

    private void UpdateMaintenance()
    {
        // ------------------------------------------
        // REPAIR FURNITURE FIRST
        // ------------------------------------------

        if (currentFurniture != null)
        {
            UpdateFurnitureRepair();
            return;
        }

        // ------------------------------------------
        // CLEANUP SECOND
        // ------------------------------------------

        if (currentMess == null)
        {
            if (canRepairFurniture)
            {
                furnitureSearchTimer -= Time.deltaTime;

                if (furnitureSearchTimer <= 0f)
                {
                    furnitureSearchTimer = furnitureSearchInterval;
                    FindFurnitureToRepair();
                }

                if (currentFurniture != null)
                    return;
            }

            if (!canCleanMess)
                return;

            ClearCurrentMess();

            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                searchTimer = messSearchInterval;
                FindMess();
            }

            return;
        }

        if (isCleaning)
        {
            UpdateCleaning();
            return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            !agent.hasPath)
        {
            ClearCurrentMess();
            return;
        }

        if (agent.remainingDistance <=
            Mathf.Max(agent.stoppingDistance, 0.4f))
        {
            StartCleaning();
        }
    }

    private void UpdateCashier()
    {
        if (!cashierConfigured)
            return;

        Debug.Log(
            $"{EmployeeName}: Cashier update running. " +
            $"AtStation={cashierAtStation}, " +
            $"Customer={cashierCustomer}, " +
            $"Attempted={cashierCheckoutAttempted}, " +
            $"ShiftEnding={shiftEndRequested}",
            this
        );

        // Walk to the checkout counter.
        if (!cashierAtStation)
        {
            if (agent.pathPending)
                return;

            float distance = Vector3.Distance(
                transform.position,
                cashierStation.position
            );

            if (distance <= Mathf.Max(agent.stoppingDistance, 0.8f))
            {
                cashierAtStation = true;
                agent.isStopped = true;

                Debug.Log(
                    $"{EmployeeName} arrived at the checkout counter. " +
                    $"QueueAssigned={checkoutQueue != null}, " +
                    $"HasCashier={checkoutQueue != null && checkoutQueue.HasCashier}, " +
                    $"CheckoutUIAssigned={cashierCheckoutUI != null}, " +
                    $"BagSpawnAssigned={cashierBagSpawn != null}",
                    this
                );
            }
            else if (!agent.hasPath ||
                    agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                agent.isStopped = false;
                agent.SetDestination(cashierStation.position);
            }

            return;
        }

        Debug.Log(
            $"{EmployeeName}: Reached customer processing section.",
            this
        );

        // Handle the customer currently being served.
        if (cashierCustomer != null)
        {
            if (cashierCustomer.IsDead)
            {
                checkoutQueue.ReleaseCustomer(this, cashierCustomer);

                cashierCustomer = null;
                cashierCheckoutAttempted = false;

                if (shiftEndRequested)
                    StartLeavingAfterCheckout();

                return;
            }

            // Payment is complete. Wait until the customer takes the bag.
            if (cashierCustomer.HasPaid)
            {
                if (cashierBagSpawn.ActiveBag == null)
                {
                    checkoutQueue.ReleaseCustomer(this, cashierCustomer);

                    cashierCustomer = null;
                    cashierCheckoutAttempted = false;

                    Debug.Log(
                        $"{EmployeeName} finished serving the customer."
                    );

                    if (shiftEndRequested)
                    {
                        StartLeavingAfterCheckout();
                        return;
                    }

                    Debug.Log(
                        $"{EmployeeName} is ready for the next customer."
                    );
                }

                return;
            }
            if (!cashierCheckoutAttempted)
            {
                cashierCheckoutAttempted = true;

                bool successfulCheckout =
                    cashierCheckoutUI.ProcessCashierCheckout(cashierCustomer);

                if (!successfulCheckout)
                {
                    Debug.LogWarning(
                        $"{EmployeeName} could not complete checkout for " +
                        $"{cashierCustomer.name}. Handing checkout to the player.",
                        this
                    );

                    // Hand the existing transaction over to the player.
                    cashierCheckoutUI.BeginCheckout(cashierCustomer);
                }
            }

            return;
        }

        // Do not accept another customer after the shift ends.
        if (shiftEndRequested)
        {
            StartLeavingAfterCheckout();
            return;
        }

        Debug.Log(
            $"{EmployeeName}: Checking queue. " +
            $"HasCashier={checkoutQueue.HasCashier}, " +
            $"ServingCustomer={checkoutQueue.ServingCustomer}, " +
            $"ActiveBag={cashierBagSpawn.ActiveBag}",
            this
        );

        Debug.Log(
            $"{EmployeeName}: Waiting for customer. " +
            $"QueueCount={checkoutQueue.CustomerCount}, " +
            $"HasCashier={checkoutQueue.HasCashier}, " +
            $"AssignedCorrectly={checkoutQueue.AssignedCashier == this}",
            this
        );

        if (checkoutQueue.TryClaimNextCustomer(
            this,
            out Customer nextCustomer))
        {
            cashierCustomer = nextCustomer;
            cashierCheckoutAttempted = false;

            Debug.Log(
                $"{EmployeeName} successfully claimed {cashierCustomer.name}.",
                this
            );
        }
    }

    private void FindMess()
    {
        StoreMess[] messes = FindObjectsOfType<StoreMess>();

        StoreMess bestMess = null;
        float bestDistance = float.MaxValue;

        foreach (StoreMess mess in messes)
        {
            if (mess == null || mess.IsClaimed)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                mess.transform.position
            );

            if (distance > messSearchRadius ||
                distance >= bestDistance)
                continue;

            // Only select messes the employee can reach.
            if (!agent.CalculatePath(
                mess.transform.position,
                navigationPath))
                continue;

            if (navigationPath.status != NavMeshPathStatus.PathComplete)
                continue;

            bestMess = mess;
            bestDistance = distance;
        }

        if (bestMess == null)
            return;

        if (!bestMess.TryClaim(this))
            return;

        currentMess = bestMess;
        agent.isStopped = false;

        if (!agent.SetDestination(currentMess.transform.position))
        {
            ClearCurrentMess();
            return;
        }

        Debug.Log(
            $"{EmployeeName} is going to clean {currentMess.Type}."
        );
    }

    private void FindFurnitureToRepair()
    {
        FixableFurniture[] furniture =
            FindObjectsOfType<FixableFurniture>();

        FixableFurniture bestFurniture = null;
        float bestDistance = float.MaxValue;

        foreach (FixableFurniture furnitureItem in furniture)
        {
            if (furnitureItem == null ||
                !furnitureItem.NeedsRepair ||
                claimedFurniture.Contains(furnitureItem))
                continue;

            float distance = Vector3.Distance(
                transform.position,
                furnitureItem.transform.position
            );

            if (distance > furnitureSearchRadius ||
                distance >= bestDistance)
                continue;

            if (!agent.CalculatePath(
                furnitureItem.transform.position,
                navigationPath))
                continue;

            if (navigationPath.status != NavMeshPathStatus.PathComplete)
                continue;

            bestFurniture = furnitureItem;
            bestDistance = distance;
        }

        if (bestFurniture == null)
            return;

        claimedFurniture.Add(bestFurniture);

        currentFurniture = bestFurniture;

        furnitureRepairDuration = Mathf.Lerp(
            slowestRepairDuration,
            fastestRepairDuration,
            Speed / 100f
        );

        agent.isStopped = false;

        if (!agent.SetDestination(currentFurniture.transform.position))
        {
            ClearFurnitureRepair();
            return;
        }

        Debug.Log(
            $"{EmployeeName} is going to repair {currentFurniture.name}.",
            this
        );
    }

    private void UpdateFurnitureRepair()
    {
        if (currentFurniture == null)
        {
            ClearFurnitureRepair();
            return;
        }

        // Another system may have repaired the furniture already.
        if (!currentFurniture.NeedsRepair)
        {
            ClearFurnitureRepair();
            return;
        }

        if (isRepairingFurniture)
        {
            furnitureRepairTimer -= Time.deltaTime;

            if (furnitureRepairTimer <= 0f)
            {
                currentFurniture.RepairByEmployee();

                Debug.Log(
                    $"{EmployeeName} repaired {currentFurniture.name}.",
                    this
                );

                ClearFurnitureRepair();
            }

            return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            !agent.hasPath)
        {
            ClearFurnitureRepair();
            return;
        }

        if (agent.remainingDistance <=
            Mathf.Max(agent.stoppingDistance, 0.4f))
        {
            StartFurnitureRepair();
        }
    }

    private void StartFurnitureRepair()
    {
        if (currentFurniture == null)
        {
            ClearFurnitureRepair();
            return;
        }

        agent.isStopped = true;

        isRepairingFurniture = true;
        furnitureRepairTimer = furnitureRepairDuration;

        Debug.Log(
            $"{EmployeeName} started repairing {currentFurniture.name}. " +
            $"Repair duration: {furnitureRepairDuration:F1} seconds.",
            this
        );
    }

    private void ClearFurnitureRepair()
    {
        if (currentFurniture != null)
            claimedFurniture.Remove(currentFurniture);

        currentFurniture = null;
        isRepairingFurniture = false;
        furnitureRepairTimer = 0f;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    private void StartCleaning()
    {
        if (currentMess == null)
        {
            ClearCurrentMess();
            return;
        }

        agent.isStopped = true;
        isCleaning = true;
        cleaningTimer = cleaningDuration;

        Debug.Log(
            $"{EmployeeName} started cleaning {currentMess.Type}."
        );
    }

    private void UpdateCleaning()
    {
        if (currentMess == null)
        {
            ClearCurrentMess();
            return;
        }

        cleaningTimer -= Time.deltaTime;

        if (cleaningTimer > 0f)
            return;

        StoreMess.MessType cleanedType = currentMess.Type;

        PlayCleaningSound(cleanedType);

        currentMess.CompleteCleanup();

        Debug.Log(
            $"{EmployeeName} cleaned {cleanedType}."
        );

        currentMess = null;
        isCleaning = false;

        agent.isStopped = false;

        searchTimer = 0.5f;
    }

    private void PlayCleaningSound(StoreMess.MessType type)
    {
        if (audioSource == null)
            return;

        AudioClip clip = type == StoreMess.MessType.Trash
            ? trashPickupSound
            : spillCleaningSound;

        if (clip != null)
        {
            audioSource.PlayOneShot(
                clip,
                cleaningVolume
            );
        }
    }

    private void ClearCurrentMess()
    {
        if (currentMess != null)
            currentMess.ReleaseClaim(this);

        currentMess = null;
        isCleaning = false;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    // --------------------------------------------------
    // ENDING THE SHIFT
    // --------------------------------------------------

    public void BeginLeavingStore(
        Transform exitPoint,
        Action onComplete)
    {
        if (leavingStore)
            return;

        leavingStore = true;
        onShiftEnded = onComplete;

        if (checkoutQueue != null)
        {
            if (cashierCustomer != null)
            {
                checkoutQueue.ReleaseCustomer(this, cashierCustomer);
                cashierCustomer = null;
            }

            checkoutQueue.AssignCashier(null);
        }

        ClearCurrentMess();
        ClearFurnitureRepair();

        if (agent == null || exitPoint == null)
        {
            FinishLeaving();
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            FinishLeaving();
            return;
        }

        agent.isStopped = false;

        if (!agent.SetDestination(exitPoint.position))
        {
            Debug.LogWarning(
                $"{EmployeeName} could not find a path to the exit."
            );

            FinishLeaving();
        }
    }

    private void UpdateLeaving()
    {
        if (agent == null || !agent.enabled ||
            !agent.isOnNavMesh)
        {
            FinishLeaving();
            return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning(
                $"{EmployeeName} could not reach the employee exit."
            );

            FinishLeaving();
            return;
        }

        if (!agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            agent.remainingDistance <= agent.stoppingDistance)
        {
            FinishLeaving();
        }
    }

    private void FinishLeaving()
    {
        Action callback = onShiftEnded;
        onShiftEnded = null;

        if (callback != null)
            callback();
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (currentMess != null)
            currentMess.ReleaseClaim(this);

        if (currentFurniture != null)
            claimedFurniture.Remove(currentFurniture);
    }
    public void ConfigureCashier(
        QueueLine queue,
        CheckoutUI checkoutUI,
        BagSpawn bagSpawn,
        Transform station
    )
    {
        if (Role != EmployeeRole.Cashier)
            return;

        checkoutQueue = queue;
        cashierCheckoutUI = checkoutUI;
        cashierBagSpawn = bagSpawn;
        cashierStation = station;

        if (checkoutQueue == null ||
            cashierCheckoutUI == null ||
            cashierBagSpawn == null ||
            cashierStation == null)
        {
            Debug.LogError(
                $"{EmployeeName}: Cashier configuration is incomplete.",
                this
            );

            return;
        }

        checkoutQueue.AssignCashier(this);

        cashierConfigured = true;
        cashierAtStation = false;
        Debug.Log(
        $"{EmployeeName}: Cashier configured successfully. " +
        $"Queue={checkoutQueue.name}, " +
        $"UI={cashierCheckoutUI.name}, " +
        $"BagSpawn={cashierBagSpawn.name}, " +
        $"Station={cashierStation.name}",
        this
    );

        Debug.Log(
            $"{EmployeeName} has been assigned to the checkout counter."
        );
    }

    private void StartLeavingAfterCheckout()
    {
        if (!shiftEndRequested || cashierCustomer != null)
            return;

        BeginLeavingStore(
            pendingExitPoint,
            pendingShiftCallback
        );

        pendingExitPoint = null;
        pendingShiftCallback = null;
    }

    public void RequestEndShift(
        Transform exitPoint,
        Action onComplete
    )
    {
        if (leavingStore)
            return;

        // Maintenance employees can leave immediately.
        if (Role != EmployeeRole.Cashier)
        {
            BeginLeavingStore(exitPoint, onComplete);
            return;
        }

        // Tell the cashier to finish the current transaction first.
        shiftEndRequested = true;
        pendingExitPoint = exitPoint;
        pendingShiftCallback = onComplete;

        Debug.Log(
            $"{EmployeeName} is finishing their current checkout before leaving."
        );

        // If nobody is being served, leave immediately.
        if (cashierCustomer == null)
            StartLeavingAfterCheckout();
    }


}
