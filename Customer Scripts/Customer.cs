using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Customer))]
public class Customer : MonoBehaviour
{
    #region Customer Decision Enums

    public enum CustomerState
    {
        Entering,
        Shopping,
        GoingToCheckout,
        GoingToReading,
        Leaving
    }

    public enum ReadingType
    {
        Love,
        Wealth,
        Health
    }

    private enum PaymentType
    {
        Cash,
        Card
    }


    #endregion

    #region Serialized Fields

    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform checkoutPoint;
    [SerializeField] private Transform readingTablePoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform[] shoppingStops;
    [SerializeField] private Animator animator;

    [Header("Customer Behavior")]
    [SerializeField] private int minShoppingStops = 1;
    [SerializeField] private int maxShoppingStops = 3;
    [SerializeField] private float shoppingWaitTime = 2f;
    [SerializeField] private CustomerDialog customerDialog;

    [Header("Products")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private float itemSpacing = 0.25f;

    [Header("Wallet")]
    [SerializeField] private int minCash = 10;
    [SerializeField] private int maxCash = 50;

    [Header("Checkout")]
    [SerializeField] private CheckoutUI checkoutUI;
    [SerializeField] private int readingFee = 10;
    [SerializeField] private ReadingCards readingCards;
    [SerializeField] private Transform bagCarryPoint;
    [SerializeField] private BagSpawn bagSpawn;
    [SerializeField] private Transform[] checkoutItemPositions;
    [SerializeField] private float checkoutItemSpacing = 0.25f;

    [Header("Checkout Queue")]
    [SerializeField] private QueueLine checkoutQueue;
    

    [Header("Checkout Patience")]
    [SerializeField] private float checkoutPatienceDuration = 30f;
    [SerializeField] private CustomerPatienceUI patienceUI;


    [Header("Fortune Telling Queue")]
    [SerializeField] private FortuneTellingLine fortuneTellingLine;

    [Header("Reputation")]
    [SerializeField] private ReputationSystem reputationSystem;

    [Header("Fire Evacuation")]
    [SerializeField, Min(0f)] private float fireDetectionDistance = 8f;
    [SerializeField, Min(0.1f)] private float fireCheckInterval = .35f;
    #endregion

    #region Public Properties

    public ReadingType PurchaseReading { get; private set; }

    public bool HasPurchasedReading { get; private set; }

    public bool HasReadingTicket { get; private set; }

    public CustomerState CurrentState { get; private set; }

    public bool WantsReading { get; private set; }

    public int ShoppingTotal => shoppingTotal;

    public int ReadingFee => readingFee;

    public bool PaysByCard => paymentType == PaymentType.Card;

    public bool CanAffordReading =>
        PaysByCard || cashAvailable >= shoppingTotal + readingFee;

    public bool HasPaid { get; private set; }

    public bool Unsatisfied { get; private set; }

    public IReadOnlyList<string> PurchasedItemNames => purchasedItemNames;

    #endregion

    #region Private State

    private PaymentType paymentType;
    private int cashAvailable;
    private int shoppingTotal;
    private ReadingType? ticketReadingType;
    private BagContainer checkoutBag;
    private float patienceRemaining;
    private Coroutine customerRoutine;

    private readonly List<GameObject> purchasedItems = new List<GameObject>();
    private readonly List<string> purchasedItemNames = new List<string>();
    private readonly List<ProductData> purchasedProducts = new List<ProductData>();
    public IReadOnlyList<ProductData> PurchasedProducts => purchasedProducts;

    private float fireCheckTimer;
    private bool isEvacuating;
    public bool IsEvacuating => isEvacuating;

    #endregion

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
        {
            Debug.LogError(
                $"{name}: Customer has no NavMeshAgent reference.",
                this
            );
        }
    }

    private void Update()
    {
        if (!isEvacuating)
        {
            fireCheckTimer += Time.deltaTime;

            if (fireCheckTimer >= fireCheckInterval)
            {
                fireCheckTimer = 0f;
                
                if (IsNearActiveFire())
                {
                    EvacuateFromFire();
                }
            }
        }
        
        if (animator == null || agent == null)
            return;

        float speed = agent.velocity.magnitude;

        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
    
    private void Start()
    {

        if(patienceUI != null)
        {
            patienceUI.SetVisible(false);
        }
        
        WantsReading = Random.value < 0.25f;

        if (WantsReading)
        {
            PurchaseReading = (ReadingType)Random.Range(0, 3);
        }

        // Each customer gets either cash or a card.
        paymentType = Random.value < 0.5f
            ? PaymentType.Cash
            : PaymentType.Card;

        cashAvailable = Random.Range(minCash, maxCash + 1);

        if (paymentType == PaymentType.Cash)
        {
            Debug.Log($"{name} has ${cashAvailable} cash.");
        }
        else
        {
            Debug.Log($"{name} is paying by card.");
        }

        customerRoutine = StartCoroutine(CustomerRoutine());
    }

    private IEnumerator CustomerRoutine()
    {
        CurrentState = CustomerState.Entering;

        // Shop at randomly selected shelf stops.
        CurrentState = CustomerState.Shopping;

        if (shoppingStops != null && shoppingStops.Length > 0)
        {
            int stopCount = Random.Range(
                minShoppingStops,
                maxShoppingStops + 1
            );

            stopCount = Mathf.Min(stopCount, shoppingStops.Length);

            // Build a list of valid stops so the customer doesn't
            // select the same stop more than once.
            List<Transform> availableStops = new List<Transform>();

            foreach (Transform stop in shoppingStops)
            {
                if (stop != null)
                {
                    availableStops.Add(stop);
                }
            }

            stopCount = Mathf.Min(stopCount, availableStops.Count);

            for (int i = 0; i < stopCount; i++)
            {
                int randomIndex = Random.Range(0, availableStops.Count);
                Transform stop = availableStops[randomIndex];

                availableStops.RemoveAt(randomIndex);

                yield return MoveTo(stop.position);

                TryTakeItemFromShelf(stop);

                yield return new WaitForSeconds(shoppingWaitTime);
            }
        }
        // After shopping, decide whether this customer needs checkout.
        bool hasProducts = purchasedItems.Count > 0;

        if (!hasProducts && !WantsReading)
        {
            // They came in but bought nothing and don't want a reading.
            yield return LeaveStoreWithoutCheckout();
            yield break;
        }

        // Customers who want a reading—even with no products—continue to checkout so they can purchase their reading ticket.

        CurrentState = CustomerState.GoingToCheckout;

        if (checkoutQueue != null)
        {
            Debug.Log($"{name}: joining checkout queue.");
            checkoutQueue.JoinQueue(this);

            patienceRemaining = Mathf.Max(0f, checkoutPatienceDuration);

            if (patienceUI != null)
            {
                patienceUI.SetVisible(true);
                patienceUI.SetPatience(1f);
            }

            // Patience only decreases while the customer is waiting in line.
            while (patienceRemaining > 0f &&
                !checkoutQueue.IsReadyForCheckout(this))
            {
                patienceRemaining -= Time.deltaTime;

                if (patienceUI != null)
                {
                    float normalizedPatience = checkoutPatienceDuration > 0f
                        ? patienceRemaining / checkoutPatienceDuration
                        : 0f;

                    patienceUI.SetPatience(normalizedPatience);
                }

                yield return null;
            }

            if (patienceUI != null)
                patienceUI.SetVisible(false);

            // If the customer has reached the front, let them finish
            // walking into position even if their patience has run out.
            if (!checkoutQueue.IsReadyForCheckout(this))
            {
                if (checkoutQueue.IsAtFront(this))
                {
                    Debug.Log($"{name}: at the front of the queue; proceeding to checkout.");

                    yield return new WaitUntil(
                        () => checkoutQueue == null ||
                            checkoutQueue.IsReadyForCheckout(this));
                }
                else
                {
                    yield return AbandonCheckoutQueue();
                    yield break;
                }
            }

            Debug.Log($"{name}: ready for checkout.");
        }
        else
        {
            Debug.Log($"{name}: no checkout queue assigned; moving directly to checkout.");

            if (checkoutPoint != null)
            {
                yield return MoveTo(checkoutPoint.position);
            }
            else
            {
                Debug.LogError($"{name}: Checkout point is not assigned.", this);
                yield break;
            }
        }

        // Register this customer with the bag spawner, place products on the counter, and begin checkout.
        BeginCustomerCheckout();

        if (customerDialog != null)
        {
            customerDialog.AtCheckout(this);
        }


        Debug.Log(
            $"{name}: Waiting for checkout. HasPaid={HasPaid}",
            this
        );

        // Wait until checkout has actually been completed.
        yield return new WaitUntil(() => HasPaid);

        Debug.Log(
            $"{name}: Payment confirmed. HasPurchasedReading={HasPurchasedReading}",
            this
        );

        // Take the bag after payment.
        TakeCheckoutBag();


        // Leave the checkout queue.
        if (checkoutQueue != null)
        {
            checkoutQueue.LeaveQueue(this);
        }

        // Visit the fortune-telling table if the reading was purchased.
        if (HasPurchasedReading)
        {
            CurrentState = CustomerState.GoingToReading;

            if (readingCards == null || fortuneTellingLine == null)
            {
                Debug.LogWarning(
                    $"{name}: ReadingCards or FortuneTellingLine is missing.",
                    this
                );
            }
            else if (readingTablePoint == null)
            {
                Debug.LogWarning(
                    $"{name}: Reading table destination is not assigned.",
                    this
                );
            }
            else
            {
                // Join the fortune-telling queue first.
                JoinFortuneTellingLine();

                // Wait until this customer is at the front and the
                // reading table is available.
                yield return new WaitUntil(() =>
                    fortuneTellingLine.IsAtFront(this) &&
                    readingCards.WaitingCustomer == null &&
                    !readingCards.HasActiveSpread
                );

                // Approach the table.
                yield return MoveTo(readingTablePoint.position);

                // Claim the table after arriving.
                readingCards.SetWaitingCustomer(this);

                Debug.Log($"{name} is ready for their reading.");

                // Wait for the player to complete the reading.
                yield return new WaitUntil(() =>
                    readingCards.IsReadingCompleteFor(this)
                );

                // Wait for the reading system to release this customer.
                yield return new WaitUntil(() =>
                    readingCards.WaitingCustomer != this
                );

                fortuneTellingLine.LeaveLine(this);
            }
        }

        // Leave the store.
        yield return LeaveStore();

    }

    #region Shopping and Carrying Products

    private void TryTakeItemFromShelf(Transform stop)
    {
        // The stop can be the shelf itself or a child of the shelf.
        ReStockShelf shelf = stop.GetComponentInParent<ReStockShelf>();

        if (shelf == null)
        {
            Debug.LogWarning($"No ReStockShelf found at shopping stop: {stop.name}");
            return;
        }

        // Read product data before TakeProduct() potentially clears it.
        ProductData productData = shelf.CurrentProductType;

        if (productData == null)
        {
            Debug.Log($"{name} found an empty shelf.");
            return;
        }

        int price = productData.sellPrice;

        // Cash customers skip products they cannot afford.
        if (paymentType == PaymentType.Cash &&
            shoppingTotal + price > cashAvailable)
        {
            Debug.Log($"{name} can't afford {productData.productName}.");
            return;
        }

        GameObject item = shelf.TakeProduct();

        if (item == null)
        {
            return;
        }

        CarryItem(item);

        purchasedItems.Add(item);
        purchasedProducts.Add(productData);
        purchasedItemNames.Add(productData.productName);
        shoppingTotal += price;

        Debug.Log($"{name} picked up {productData.productName} for ${price}.");
    }

    private void CarryItem(GameObject item)
    {
        Transform parent = carryPoint != null
            ? carryPoint
            : transform;

        item.transform.SetParent(parent);

        // Position this product in the customer's carried stack.
        int itemIndex = purchasedItems.Count;

        item.transform.localPosition = new Vector3(
            0f,
            itemIndex * itemSpacing,
            0f
        );

        item.transform.localRotation = Quaternion.identity;

        // Disable collisions while the customer carries the product.
        foreach (Collider itemCollider in item.GetComponentsInChildren<Collider>())
        {
            itemCollider.enabled = false;
        }

        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    #endregion

    #region Reading Ticket

    public bool GiveReadingTicket(ReadingType ticketType)
    {
        if (!WantsReading || HasPaid)
        {
            return false;
        }

        HasReadingTicket = true;
        ticketReadingType = ticketType;

        return true;
    }

    public void ClearReadingTicket()
    {
        HasReadingTicket = false;
        ticketReadingType = null;
    }

    #endregion

    #region Movement

    private bool moveToSucceeded;

    private IEnumerator MoveTo(Vector3 destination)
    {
        moveToSucceeded = false;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name}: Agent missing, disabled, or off NavMesh.",
                this
            );
            yield break;
        }

        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(destination, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning(
                $"{name}: No complete path to {destination}.",
                this
            );
            yield break;
        }

        agent.isStopped = false;

        if (!agent.SetPath(path))
        {
            Debug.LogWarning($"{name}: Could not assign path.", this);
            yield break;
        }

        float elapsed = 0f;
        const float timeout = 30f;

        while (elapsed < timeout)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning(
                    $"{name}: Agent became unavailable during movement.",
                    this
                );
                yield break;
            }

            if (agent.pathPending)
            {
                elapsed += Time.deltaTime;
                yield return null;
                continue;
            }

            float arrivalTolerance =
                Mathf.Max(agent.stoppingDistance + 0.35f, 0.6f);

            bool hasKnownDistance =
                !float.IsInfinity(agent.remainingDistance) &&
                !float.IsNaN(agent.remainingDistance);

            bool reachedByPath =
                hasKnownDistance &&
                agent.remainingDistance <= arrivalTolerance;

            bool reachedByPosition =
                Vector3.Distance(agent.nextPosition, destination)
                <= arrivalTolerance;

            bool isNearlyStopped =
                agent.velocity.sqrMagnitude < 0.04f;

            // Do not require agent.hasPath: Unity may clear the path
            // automatically when the agent reaches its destination.
            if ((reachedByPath || reachedByPosition) && isNearlyStopped)
            {
                agent.isStopped = true;
                moveToSucceeded = true;

                Debug.Log(
                    $"{name}: Reached destination {destination}.",
                    this
                );

                yield break;
            }

            if (agent.hasPath &&
                agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning(
                    $"{name}: Navigation path became invalid.",
                    this
                );
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(
            $"{name}: Movement timed out. " +
            $"Position={transform.position}, " +
            $"Destination={destination}, " +
            $"RemainingDistance={agent.remainingDistance}, " +
            $"HasPath={agent.hasPath}, " +
            $"Velocity={agent.velocity.magnitude:F2}",
            this
        );
    }


    public void MoveToQueuePosition(Vector3 position)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = false;

        // Avoid recalculating the destination unnecessarily.
        if (!agent.hasPath ||
            Vector3.Distance(agent.destination, position) > 0.2f)
        {
            agent.SetDestination(position);
        }
    }

    public bool IsNearQueuePosition(Vector3 position)
    {
        if (agent == null || !agent.isOnNavMesh || agent.pathPending)
        {
            return false;
        }

        float arrivalTolerance = Mathf.Max(
            agent.stoppingDistance + 0.5f,
            0.75f
        );

        return agent.hasPath &&
               agent.remainingDistance <= arrivalTolerance;
    }

    #endregion

    #region Checkout and Payment

    public bool TryCompleteCheckout(
        bool payingByCard,
        bool includeReading,
        ReadingType readingType,
        out int total,
        out string message)
    {
        total = 0;
        message = string.Empty;

        if (HasPaid)
        {
            message = "This customer has already paid.";
            return false;
        }

        // Do not allow payment until every purchased item is in this
        // customer's assigned checkout bag.
        if (!AreAllItemsBagged())
        {
            message = "Bag all purchased items before completing checkout.";
            return false;
        }

        if (payingByCard != PaysByCard)
        {
            message = PaysByCard
                ? "This customer needs to pay by card."
                : "This customer needs to pay with cash.";

            return false;
        }

        bool readingCorrect = false;

        if (includeReading)
        {
            if (!WantsReading || !HasReadingTicket)
            {
                message = "A reading ticket must be printed first.";
                return false;
            }

            readingCorrect =
                ticketReadingType == PurchaseReading &&
                readingType == PurchaseReading;

            if (!readingCorrect)
            {
                Unsatisfied = true;
            }
        }

        // Charge the reading fee only when the reading ticket is correct.
        total = shoppingTotal + (readingCorrect ? readingFee : 0);

        if (!PaysByCard && total > cashAvailable)
        {
            message = "The customer doesn't have enough cash.";
            total = 0;
            return false;
        }

        if (!PaysByCard)
        {
            cashAvailable -= total;
        }

        HasPurchasedReading = readingCorrect;
        HasPaid = true;

        // Bagging was validated above. Clear this purchase list after
        // successful payment; the products remain inside the bag.
        purchasedItems.Clear();

        message = Unsatisfied
            ? $"Payment accepted for products only: ${total}. Customer is unsatisfied."
            : $"Payment accepted: ${total}.";

        return true;
    }

    public void BeginCustomerCheckout()
    {
        if (bagSpawn != null)
        {
            // BagSpawn should spawn/assign this customer's bag here.
            bagSpawn.SetCheckoutCustomer(this);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: BagSpawn is not assigned. Checkout will remain blocked until a bag is assigned.",
                this
            );
        }

        PlaceItemsOnCounter();

        if (checkoutUI != null)
        {
            checkoutUI.BeginCheckout(this);
        }
        else
        {
            Debug.LogError($"{name}: CheckoutUI is not assigned.", this);
        }
    }

    public void PlaceItemsOnCounter()
    {
        if (purchasedItems.Count == 0)
        {
            Debug.LogWarning($"{name} has no purchased items to place.", this);
            return;
        }

        if (checkoutItemPositions == null ||
            checkoutItemPositions.Length == 0)
        {
            Debug.LogError(
                $"{name} has no checkout item positions assigned in the Inspector.",
                this
            );
            return;
        }

        for (int i = 0; i < purchasedItems.Count; i++)
        {
            GameObject item = purchasedItems[i];

            if (item == null)
            {
                continue;
            }

            // Use a designated position. Extra items are offset to the side.
            Transform placement = checkoutItemPositions[
                Mathf.Min(i, checkoutItemPositions.Length - 1)
            ];

            Vector3 extraOffset = Vector3.zero;

            if (i >= checkoutItemPositions.Length)
            {
                int extraIndex = i - checkoutItemPositions.Length + 1;

                extraOffset =
                    placement.right * (checkoutItemSpacing * extraIndex);
            }

            // Detach from the customer's carried-item hierarchy.
            item.transform.SetParent(null, true);

            item.transform.position = placement.position + extraOffset;
            item.transform.rotation = placement.rotation;

            // Enable colliders so the player can target the checkout item.
            foreach (Collider col in item.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
            }

            Rigidbody rb = item.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            int checkoutLayer = LayerMask.NameToLayer("CheckoutItem");

            if (checkoutLayer >= 0)
            {
                SetLayerRecursively(item.transform, checkoutLayer);
            }
            else
            {
                Debug.LogWarning(
                    "Create a Unity layer named CheckoutItem.",
                    item
                );
            }
        }
    }
    private IEnumerator LeaveStoreWithoutCheckout()
    {
        Debug.Log($"{name}: leaving without purchasing anything.");
        yield return LeaveStore();

    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    public void AssignCheckoutBag(BagContainer bag)
    {
        checkoutBag = bag;

        Debug.Log(
            bag != null
                ? $"{name}: Checkout bag assigned."
                : $"{name}: Checkout bag reference cleared.",
            this
        );
    }

    public bool AreAllItemsBagged()
    {
        // A customer with no products has nothing to bag.
        if (purchasedItems.Count == 0)
        {
            return true;
        }

        if (checkoutBag == null)
        {
            return false;
        }

        return checkoutBag.ContainsAll(purchasedItems);
    }

    private void TakeCheckoutBag()
    {
        if (checkoutBag == null)
        {
            Debug.LogWarning($"{name} has no checkout bag to take.", this);
            return;
        }

        Transform carryTarget = bagCarryPoint != null
            ? bagCarryPoint
            : carryPoint;

        if (carryTarget == null)
        {
            Debug.LogWarning(
                $"{name} needs a bagCarryPoint or carryPoint.",
                this
            );
            return;
        }

        BagContainer bagBeingTaken = checkoutBag;
        Transform bagTransform = bagBeingTaken.transform;

        bagTransform.SetParent(carryTarget);
        bagTransform.localPosition = Vector3.zero;
        bagTransform.localRotation = Quaternion.identity;

        // Disable bag collisions while the customer carries it.
        foreach (Collider col in bagTransform.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        Rigidbody rb = bagTransform.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Tell the bag spawner this bag has been taken, so it can
        // prepare a bag for the next customer.
        if (bagSpawn != null)
        {
            bagSpawn.ClearTakenBag(bagBeingTaken);
        }

        checkoutBag = null;
    }

    #endregion

    #region Scene Reference Setters

    public void InitializeCheckout(
        Transform[] itemPositions,
        BagSpawn bagSpawner)
    {
        checkoutItemPositions = itemPositions;
        bagSpawn = bagSpawner;
    }

    public void SetCheckoutQueue(QueueLine queue)
    {
        checkoutQueue = queue;
    }

    public void SetExitPoint(Transform point)
    {
        exitPoint = point;
    }

    public void SetShoppingStops(Transform[] stops)
    {
        shoppingStops = stops;
    }

    public void SetCheckoutUI(CheckoutUI ui)
    {
        checkoutUI = ui;
    }

    public void SetCustomerDialog(CustomerDialog dialog)
    {
        customerDialog = dialog;
    }

    public void SetReadingCards(ReadingCards cards)
    {
        readingCards = cards;
    }

    public void SetFortuneTellingLine(FortuneTellingLine line)
    {
        fortuneTellingLine = line;
    }

    public void SetBagSpawn(BagSpawn spawner)
    {
        bagSpawn = spawner;
    }

    public void JoinFortuneTellingLine()
    {
        if (fortuneTellingLine == null)
        {
            Debug.LogWarning($"{name}: Fortune telling line is not assigned.", this);
            return;
        }

        fortuneTellingLine.JoinLine(this);
    }

    public bool TryBagCheckoutItem(GameObject hitObject)
    {
        if (hitObject == null || checkoutBag == null)
            return false;

        // Find the purchased item that was actually targeted.
        foreach (GameObject item in purchasedItems)
        {
            if (item == null)
                continue;

            bool isTargetedItem =
                hitObject == item ||
                hitObject.transform.IsChildOf(item.transform);

            if (!isTargetedItem)
                continue;

            if (checkoutBag.ContainsItem(item))
            {
                Debug.Log($"{item.name} is already bagged.");
                return false;
            }

            checkoutBag.AddItem(item);

            Debug.Log($"{name}: Bagged {item.name}.");
            return true;
        }

        Debug.Log(
            $"{name}: This item does not belong to the current customer's purchase."
        );

        return false;
    }

    public void SetReputationSystem(ReputationSystem system)
    {
        reputationSystem = system;
    }


    #endregion


    private IEnumerator AbandonCheckoutQueue()
    {
        Debug.LogWarning(
            $"{name} ran out of patience and abandoned their purchase."
        );

        CurrentState = CustomerState.Leaving;

        // Remove the customer from both queues.
        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);

        // The customer has not paid, so drop every purchased product.
        DropPurchasedItems();

        // Do not count the abandoned purchase as a sale.
        HasPaid = false;
        HasPurchasedReading = false;
        ClearReadingTicket();

        if (exitPoint != null)
            yield return MoveTo(exitPoint.position);

        Destroy(gameObject);
    }

    private void DropPurchasedItems()
    {
        for (int i = 0; i < purchasedItems.Count; i++)
        {
            GameObject item = purchasedItems[i];

            if (item == null)
                continue;

            // Preserve the ProductData belonging to this specific item.
            ProductData data = i < purchasedProducts.Count
                ? purchasedProducts[i]
                : null;

            item.transform.SetParent(null, true);

            // Scatter products around the customer's current position.
            Vector2 scatter = Random.insideUnitCircle * 0.6f;

            item.transform.position = transform.position +
                new Vector3(scatter.x, 0.15f, scatter.y);

            // Ensure the item can be picked up from the floor.
            DroppedProduct dropped = item.GetComponent<DroppedProduct>();

            if (dropped == null)
                dropped = item.AddComponent<DroppedProduct>();

            dropped.Initialize(data);
            dropped.PrepareForPickup();

            // Ensure a rigidbody exists so the item can fall onto the floor.
            Rigidbody rb = item.GetComponent<Rigidbody>();

            if (rb == null)
                rb = item.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;

            // Product colliders are re-enabled by PrepareForPickup().
            Debug.Log(
                $"{name} dropped {item.name} after running out of patience."
            );
        }

        // Clear the abandoned purchase records.
        purchasedItems.Clear();
        purchasedProducts.Clear();
        purchasedItemNames.Clear();
        shoppingTotal = 0;
    }

    #region Possession Logic

    public void PrepareForPossession()
    {
        // Stop the customer's normal AI routine immediately.
        if (customerRoutine != null)
        {
            StopCoroutine(customerRoutine);
            customerRoutine = null;
        }

        // Cancel checkout before dropping the products.
        // This also releases the UI for the next customer.
        if (checkoutUI != null)
            checkoutUI.CancelCheckout(this);

        // Drop all unpaid products using the existing patience-timeout logic.
        DropPurchasedItems();

        // Clear unpaid transaction state.
        HasPaid = false;
        HasPurchasedReading = false;
        ClearReadingTicket();

        // Remove this customer from both queues.
        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);

        // Hide the patience UI.
        if (patienceUI != null)
            patienceUI.SetVisible(false);

        // Stop navigation.
        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        Debug.Log(
            $"{name}: possession interrupted normal behavior and checkout.",
            this
        );
    }

    public void RemoveFromStoreAfterPossession()
    {
        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);

        if (patienceUI != null)
            patienceUI.SetVisible(false);

        // This is temporary cleanup. We will replace it with the physical store-boundary check and ragdoll/carry cleanup in the throw system.
        Destroy(gameObject);
    }
    #endregion

    public bool IsDead { get; private set; }

    public void PrepareForDeath()
    {
        if (IsDead)
            return;

        // Mark dead first so other systems can recognize this immediately.
        IsDead = true;

        // Stop shopping, queue waiting, checkout waiting, and reading routines.
        StopAllCoroutines();
        customerRoutine = null;

        // Disable the possession kill behavior.
        PossessionKillAI killAI = GetComponent<PossessionKillAI>();

        if (killAI != null)
            killAI.enabled = false;

        // Cancel checkout if this customer currently owns the checkout UI. CancelCheckout also releases the BagSpawn checkout assignment.
        if (checkoutUI != null)
            checkoutUI.CancelCheckout(this);

        // Remove the customer from both queues immediately.
        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);

        // Hide their patience indicator.
        if (patienceUI != null)
            patienceUI.SetVisible(false);

        // Stop navigation without destroying the customer GameObject.
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        Debug.Log(
            $"{name}: Death interrupted customer behavior and removed them from all queues and checkout.",
            this
        );
    }

    private IEnumerator LeaveStore()
    {
        CurrentState = CustomerState.Leaving;

        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);

        if (exitPoint == null)
        {
            Debug.LogWarning($"{name}: Exit point is not assigned.", this);
            yield break;
        }

        yield return MoveTo(exitPoint.position);

        if (IsDead)
            yield break;

        if (moveToSucceeded)
        {
            Debug.Log($"{name}: Reached exit. Destroying customer.", this);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: Failed to reach exit. Customer will remain in the scene.",
                this
            );
        }
    }

    private bool IsNearActiveFire()
    {
        // Customers currently possessed should continue their possession behavior.
        PossessedCustomer possessedCustomer =
            GetComponent<PossessedCustomer>();

        if (possessedCustomer != null &&
            possessedCustomer.IsPossessed)
        {
            return false;
        }

        foreach (FireHazard fire in FireHazard.ActiveFires)
        {
            if (fire == null || !fire.IsLit)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                fire.transform.position
            );

            if (distance <= fireDetectionDistance)
                return true;
        }

        return false;
    }

    public void EvacuateFromFire()
    {
        if (isEvacuating || IsDead)
            return;

        PossessedCustomer possessedCustomer =
            GetComponent<PossessedCustomer>();

        if (possessedCustomer != null &&
            possessedCustomer.IsPossessed)
        {
            return;
        }

        isEvacuating = true;
        CurrentState = CustomerState.Leaving;

        // Stop the normal shopping, checkout, or reading routine.
        if (customerRoutine != null)
        {
            StopCoroutine(customerRoutine);
            customerRoutine = null;
        }

        // Release checkout UI if this customer was being checked out.
        if (checkoutUI != null)
            checkoutUI.CancelCheckout(this);

        // Drop unpaid products rather than taking them out of the store.
        if (!HasPaid)
        {
            DropPurchasedItems();
            ClearReadingTicket();
        }

        // Remove this customer from both queues.
        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(this);

        if (fortuneTellingLine != null)
            fortuneTellingLine.LeaveLine(this);
        
        if(readingCards != null)
            readingCards.CancelReadingFor(this);

        // Hide their patience UI.
        if (patienceUI != null)
            patienceUI.SetVisible(false);

        StartCoroutine(FireEvacuationRoutine());
    }

    private IEnumerator FireEvacuationRoutine()
    {
        if (exitPoint == null)
        {
            Debug.LogWarning(
                $"{name}: Cannot evacuate because the exit point is missing.",
                this
            );

            yield break;
        }

        yield return MoveTo(exitPoint.position);

        if (moveToSucceeded)
        {
            Debug.Log($"{name} evacuated the store because of a fire.", this);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning(
                $"{name} could not reach the exit during fire evacuation.",
                this
            );
        }
    }

}