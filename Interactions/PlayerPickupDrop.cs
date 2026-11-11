using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerPickupDrop : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask pickUpLayerMask; //objects grabable
    [SerializeField] private float interactDistance = 30f;

    [Header("Checkout Bagging")]
    [SerializeField] private BagSpawn bagSpawn;
    [SerializeField] private LayerMask checkoutItemLayerMask; //items can bag
    [SerializeField] private float bagHoldDuration = 1f;
    [SerializeField] private float itemMoveDuration = 0.3f;

    [SerializeField] private PlayerHotbar hotbar;
    [SerializeField] private PlayerPossessionInteraction possessionInteraction;
    
    [Header("Hammer Repair")]
    [SerializeField] private LayerMask furnitureRepairLayerMask;
    [SerializeField] private float hammerRepairDistance = 3f;
    [SerializeField] private float hammerHitDelay = 0.2f;

    private Coroutine hammerCoroutine;
    public ObjectGrabbable HeldObject => hotbar != null ? hotbar.HeldObject : null;
    private Coroutine baggingCoroutine;
    private FireExtinguisherSpray previouslyHeldExtinguisher;



    private void Update()
    {
        FurniturePlacementManager placementManager =
            FurniturePlacementManager.Instance;

        // HANDLE FURNITURE PLACEMENT INPUT
        if (placementManager != null &&
            placementManager.IsPlacementActive)
        {
            // Q rotates furniture.
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("PlayerPickupDrop: Rotating furniture.");
                placementManager.RotatePlacement();
            }

            // Right-click confirms placement.
            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("PlayerPickupDrop: Confirming furniture placement.");
                placementManager.ConfirmPlacementInput();
            }

            // Escape cancels placement.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("PlayerPickupDrop: Cancelling furniture placement.");
                placementManager.CancelPlacementInput();
            }

            // Don't execute normal player interactions during placement.
            return;
        }

        // NORMAL PLAYER INPUT
        if (Input.GetKeyDown(KeyCode.Q))
            OpenHeldProductBox();

        UpdateHeldTool();

        // Right-click moves furniture while holding the hammer.
        if (Input.GetMouseButtonDown(1))
            TryStartFurnitureMove();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (TryStartBagging())
                return;

            HandleSingleEInteraction();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            if (baggingCoroutine != null)
            {
                StopCoroutine(baggingCoroutine);
                baggingCoroutine = null;
            }
        }
    }

    private bool TryStartBagging()
    {
        if (bagSpawn == null || bagSpawn.ActiveBag == null)
            return false;

        if (!Physics.Raycast(
                playerCameraTransform.position,
                playerCameraTransform.forward,
                out RaycastHit hit,
                interactDistance,
                checkoutItemLayerMask))
        {
            return false;
        }

        ObjectGrabbable target = hit.collider.GetComponentInParent<ObjectGrabbable>();

        if (target == null || target == HeldObject)
            return false;

        // Don't bag something currently held by the player.
        if (HeldObject == target)
            return false;

        baggingCoroutine = StartCoroutine(BagItemRoutine(target));
        return true;
    }

    private IEnumerator BagItemRoutine(ObjectGrabbable target)
    {
        float heldTime = 0f;

        // Require E to remain held for the configured duration.
        while (Input.GetKey(KeyCode.E) && heldTime < bagHoldDuration)
        {
            heldTime += Time.deltaTime;
            yield return null;
        }

        if (!Input.GetKey(KeyCode.E))
        {
            baggingCoroutine = null;
            yield break;
        }

        BagContainer bag = bagSpawn.ActiveBag;

        if (bag == null || target == null)
        {
            baggingCoroutine = null;
            yield break;
        }

        GameObject item = target.gameObject;
        Vector3 startPosition = item.transform.position;
        Quaternion startRotation = item.transform.rotation;
        Vector3 destination = bag.GetNextItemWorldPosition();

        // Keep the item from falling or being pushed while it moves into the bag.
        Rigidbody rb = item.GetComponent<Rigidbody>();
        bool hadRigidbody = rb != null;
        bool wasKinematic = hadRigidbody && rb.isKinematic;
        bool usedGravity = hadRigidbody && rb.useGravity;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        float moveTime = 0f;

        while (moveTime < itemMoveDuration && item != null)
        {
            moveTime += Time.deltaTime;
            float t = Mathf.Clamp01(moveTime / itemMoveDuration);

            item.transform.position = Vector3.Lerp(startPosition, destination, t);
            yield return null;
        }

        if (item != null)
        {
            item.transform.position = destination;
            item.transform.rotation = startRotation;

            // Register it with the bag so checkout validation can recognize it.
            bag.AddItem(item);

            // Keep the bagged item stable.
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        baggingCoroutine = null;
    }


    private void HandleSingleEInteraction()
    {
        if (Physics.Raycast(
                playerCameraTransform.position,
                playerCameraTransform.forward,
                out RaycastHit hit,
                interactDistance,
                pickUpLayerMask))
        {
            if (possessionInteraction != null &&
                possessionInteraction.IsTargetingPossessedCustomer)
                return;

            // Existing world interactions.
            Card3D tarotCard = hit.collider.GetComponentInParent<Card3D>();
            if (tarotCard != null) { tarotCard.Interact(); return; }

            ReadingCards readingDeck = hit.collider.GetComponentInParent<ReadingCards>();
            if (readingDeck != null) { readingDeck.Interact(); return; }

            Button button = hit.collider.GetComponentInParent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }

            StoreSignInteraction storeSign = hit.collider.GetComponentInParent<StoreSignInteraction>();
            if (storeSign != null) { storeSign.Interact(); return; }

            SleepInteraction bed = hit.collider.GetComponentInParent<SleepInteraction>();
            if (bed != null) { bed.Interact(); return; }

            LaptopInteract laptop = hit.collider.GetComponentInParent<LaptopInteract>();
            if (laptop != null) { laptop.Interact(); return; }

            ObjectGrabbable grabbable = hit.collider.GetComponentInParent<ObjectGrabbable>();
            if (grabbable != null)
            {
                if (HeldObject == null)
                    hotbar.TryPickup(grabbable);
                else
                    hotbar.DropSelectedItem();

                return;
            }
        }

        // If nothing usable is targeted, let the player throw/drop the held item.
        if (HeldObject != null)
            hotbar.DropSelectedItem();
    }

    public void ForgetHeldObject(ObjectGrabbable expectedObject)
    {
        if (hotbar != null)
            hotbar.ForgetItem(expectedObject);
    }


    public void EDrop()
    {
        if (hotbar != null)
            hotbar.DropSelectedItem();
    }

    private void OpenHeldProductBox()
    {
        ObjectGrabbable held = HeldObject;

        if (held == null)
            return;

        ProductBox box = held.GetComponent<ProductBox>();

        if (box != null && !box.IsOpened)
            box.OpenBox();
    }


    private void UpdateHeldTool()
    {
        ObjectGrabbable held = HeldObject;

        // FIRE EXTINGUISHER
        FireExtinguisherSpray extinguisher = held != null
            ? held.GetComponentInChildren<FireExtinguisherSpray>()
            : null;

        if (previouslyHeldExtinguisher != null &&
            previouslyHeldExtinguisher != extinguisher)
        {
            previouslyHeldExtinguisher.SetHeld(false);
        }

        previouslyHeldExtinguisher = extinguisher;

        if (extinguisher != null)
        {
            extinguisher.SetHeld(true);
            extinguisher.SetSpraying(Input.GetMouseButton(0));
        }

        // HAMMER
        HammerTool hammer = held != null
            ? held.GetComponentInChildren<HammerTool>()
            : null;

        // Stop hammer use if it is no longer being held.
        if (hammer == null && hammerCoroutine != null)
        {
            StopCoroutine(hammerCoroutine);
            hammerCoroutine = null;
        }

        // Start swinging when left-click is pressed.
        if (hammer != null &&
            Input.GetMouseButtonDown(0) &&
            hammerCoroutine == null)
        {
            hammerCoroutine = StartCoroutine(HammerUseRoutine(hammer));
        }

        // Stop repairing when left-click is released.
        if (Input.GetMouseButtonUp(0) && hammerCoroutine != null)
        {
            StopCoroutine(hammerCoroutine);
            hammerCoroutine = null;
        }
    }


    private IEnumerator HammerUseRoutine(HammerTool hammer)
    {
        FixableFurniture target = GetTargetedFixableFurniture();

        // Always perform an initial swing.
        hammer.Swing();

        // No damaged furniture targeted? Only swing once.
        if (target == null)
        {
            hammerCoroutine = null;
            yield break;
        }

        if (hammerHitDelay > 0f)
            yield return new WaitForSeconds(hammerHitDelay);

        while (Input.GetMouseButton(0))
        {
            target = GetTargetedFixableFurniture();

            if (target == null)
                break;

            target.TryRepairHit();

            if (target.IsFixed)
                break;

            yield return new WaitForSeconds(hammer.SwingInterval);

            if (!Input.GetMouseButton(0))
                break;

            hammer.Swing();

            if (hammerHitDelay > 0f)
                yield return new WaitForSeconds(hammerHitDelay);
        }

        hammerCoroutine = null;
    }

    private FixableFurniture GetTargetedFixableFurniture()
    {
        if (playerCameraTransform == null)
        {
            Debug.LogWarning("Hammer: Player camera transform is missing!");
            return null;
        }

        Ray ray = new Ray(
            playerCameraTransform.position,
            playerCameraTransform.forward
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            hammerRepairDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        ObjectGrabbable held = HeldObject;

        foreach (RaycastHit hit in hits)
        {
            if (held != null &&
                (hit.collider.transform == held.transform ||
                hit.collider.transform.IsChildOf(held.transform)))
            {
                continue;
            }

            Debug.Log("Hammer raycast hit: " + hit.collider.name);

            FixableFurniture target =
                hit.collider.GetComponentInParent<FixableFurniture>();

            if (target == null)
            {
                Debug.Log("This object does not have FixableFurniture.");
                continue;
            }

            Debug.Log(
                $"Found furniture: {target.name} | " +
                $"Health: {target.CurrentHealth}/{target.MaxHealth} | " +
                $"Needs repair: {target.NeedsRepair}"
            );

            if (target.NeedsRepair)
                return target;
        }

        Debug.Log("Hammer: No damaged furniture found in front of player.");
        return null;
    }

    public bool BeginBoxPlacement(ProductBox box, out int originalSlot)
    {
        originalSlot = -1;

        if (box == null || hotbar == null)
            return false;

        ObjectGrabbable held = HeldObject;

        if (held == null || held.GetComponent<ProductBox>() != box)
            return false;

        return hotbar.TemporarilyRemoveHeldItem(
            held,
            out originalSlot
        );
    }

    public void CompleteBoxPlacement()
    {
        if (hotbar != null)
            hotbar.SetSelectionLocked(false);
    }

    public void CancelBoxPlacement(ProductBox box, int originalSlot)
    {
        if (box == null || hotbar == null)
        {
            if (hotbar != null)
                hotbar.SetSelectionLocked(false);

            return;
        }

        ObjectGrabbable boxObject = box.GetComponent<ObjectGrabbable>();

        if (boxObject != null)
        {
            if (!hotbar.RestoreTemporarilyRemovedItem(
                boxObject,
                originalSlot))
            {
                hotbar.SetSelectionLocked(false);
            }
        }
        else
        {
            hotbar.SetSelectionLocked(false);
            Debug.LogWarning(
                "The delivery box needs an ObjectGrabbable component to return to the hotbar.",
                box
            );
        }
    }

    private void TryStartFurnitureMove()
    {
        ObjectGrabbable held = HeldObject;

        if (held == null)
            return;

        HammerTool hammer = held.GetComponentInChildren<HammerTool>();

        if (hammer == null)
            return;

        if (playerCameraTransform == null)
            return;

        Ray ray = new Ray(
            playerCameraTransform.position,
            playerCameraTransform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            hammerRepairDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
            return;

        PlaceableFurniture furniture =
            hit.collider.GetComponentInParent<PlaceableFurniture>();

        if (furniture == null)
            return;

        if (FurniturePlacementManager.Instance != null)
        {
            FurniturePlacementManager.Instance.BeginMovingFurniture(furniture);
        }
    }


}