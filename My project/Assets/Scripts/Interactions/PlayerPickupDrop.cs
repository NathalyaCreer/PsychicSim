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

    public ObjectGrabbable HeldObject => hotbar != null ? hotbar.HeldObject : null;
    private Coroutine baggingCoroutine;
    private FireExtinguisherSpray previouslyHeldExtinguisher;


    private void Update()
    {
        // Q opens the product box currently held by the player.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OpenHeldProductBox();
        }

        // Update extinguisher spray continuously while E is held.
        UpdateHeldTool();

        // Handle E only once per press.
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (TryStartBagging())
                return;

            HandleSingleEInteraction();
        }

        // Cancel bagging if E is released before it completes.
        if (Input.GetKeyUp(KeyCode.E) && baggingCoroutine != null)
        {
            StopCoroutine(baggingCoroutine);
            baggingCoroutine = null;
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

        FireExtinguisherSpray extinguisher = held != null
            ? held.GetComponentInChildren<FireExtinguisherSpray>()
            : null;

        // If the player switched tools or threw the extinguisher, release the previous extinguisher.
        if (previouslyHeldExtinguisher != null &&
            previouslyHeldExtinguisher != extinguisher)
        {
            previouslyHeldExtinguisher.SetHeld(false);
        }

        previouslyHeldExtinguisher = extinguisher;

        if (extinguisher == null)
            return;

        extinguisher.SetHeld(true);
        extinguisher.SetSpraying(Input.GetMouseButton(0));
    }


}