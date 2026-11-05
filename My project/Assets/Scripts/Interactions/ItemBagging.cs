using UnityEngine;
using System.Collections;

public class ItemBagging : MonoBehaviour
{
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private LayerMask checkoutItemLayer;
    [SerializeField] private BagSpawn bagSpawn;

    [SerializeField] private float bagDistance = 30f;
    [SerializeField] private float holdDuration = 1f;

    private GameObject currentItem;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    private Coroutine baggingCoroutine;
    private bool isHoldingE;

    public bool IsBaggingTarget(Collider hitCollider)
    {
        if (hitCollider == null || bagSpawn == null || bagSpawn.ActiveBag == null)
            return false;

        ObjectGrabbable grabbable =
            hitCollider.GetComponentInParent<ObjectGrabbable>();

        if (grabbable == null)
            return false;

        // Don't bag an item that's currently in the player's hand.
        PlayerPickupDrop pickup = GetComponent<PlayerPickupDrop>();
        if (pickup != null && pickup.HeldObject == grabbable)
            return false;

        return true;
    }

    public void HandleEInput(bool eIsHeld)
    {
        isHoldingE = eIsHeld;

        if (!eIsHeld)
        {
            CancelBagging();
            return;
        }

        // Don't start another bagging action while one is running.
        if (baggingCoroutine != null)
            return;

        if (playerCameraTransform == null || bagSpawn == null ||
            bagSpawn.ActiveBag == null)
            return;

        if (!Physics.Raycast(
            playerCameraTransform.position,
            playerCameraTransform.forward,
            out RaycastHit hit,
            bagDistance,
            checkoutItemLayer))
        {
            return;
        }

        if (!IsBaggingTarget(hit.collider))
            return;

        ObjectGrabbable grabbable =
            hit.collider.GetComponentInParent<ObjectGrabbable>();

        currentItem = grabbable.gameObject;

        originalPosition = currentItem.transform.position;
        originalRotation = currentItem.transform.rotation;
        originalParent = currentItem.transform.parent;

        baggingCoroutine = StartCoroutine(BagItemRoutine());
    }

    private IEnumerator BagItemRoutine()
    {
        BagContainer bag = bagSpawn.ActiveBag;

        if (bag == null)
        {
            baggingCoroutine = null;
            yield break;
        }

        Vector3 startPosition = currentItem.transform.position;
        Quaternion startRotation = currentItem.transform.rotation;
        Vector3 targetPosition = bag.GetNextItemWorldPosition();
        Quaternion targetRotation = bag.Interior.rotation;

        float elapsed = 0f;

        while (elapsed < holdDuration)
        {
            if (!isHoldingE)
            {
                RestoreItem();
                baggingCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / holdDuration);

            // Smooth movement into the bag while E is held.
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            currentItem.transform.position =
                Vector3.Lerp(startPosition, targetPosition, easedProgress);

            currentItem.transform.rotation =
                Quaternion.Slerp(startRotation, targetRotation, easedProgress);

            yield return null;
        }

        if (isHoldingE && currentItem != null && bagSpawn.ActiveBag != null)
        {
            bagSpawn.ActiveBag.AddItem(currentItem);
            Debug.Log($"Bagged: {currentItem.name}");
        }
        else
        {
            RestoreItem();
        }

        currentItem = null;
        baggingCoroutine = null;
    }

    private void CancelBagging()
    {
        if (baggingCoroutine != null)
        {
            StopCoroutine(baggingCoroutine);
            baggingCoroutine = null;
        }

        RestoreItem();
    }

    private void RestoreItem()
    {
        if (currentItem == null)
            return;

        currentItem.transform.SetParent(originalParent);
        currentItem.transform.position = originalPosition;
        currentItem.transform.rotation = originalRotation;
        currentItem = null;
    }
}
