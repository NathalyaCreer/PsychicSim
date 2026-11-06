
using UnityEngine;

public class BagSpawn : MonoBehaviour
{
    [Header("Bag Setup")]
    [SerializeField] private GameObject bagPrefab;
    [SerializeField] private Transform bagSpawnPoint;

    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    public BagContainer ActiveBag { get; private set; }

    private Customer checkoutCustomer;


    private void Update()
    {
        if (!Input.GetKeyDown(interactionKey))
            return;

        if (playerCamera == null)
        {
            Debug.LogWarning("BagSpawn: Player camera is not assigned.", this);
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Press E on the bag spawner to spawn a bag.
        if (hit.transform == transform || hit.transform.IsChildOf(transform))
        {
            SpawnBag();
            return;
        }

        // Press E on a checkout item to place it in the active bag.
        if (checkoutCustomer == null)
        {
            Debug.Log("No customer is currently checking out.");
            return;
        }

        if (ActiveBag == null)
        {
            Debug.Log("Spawn a bag before bagging checkout items.");
            return;
        }

        bool bagged = checkoutCustomer.TryBagCheckoutItem(hit.collider.gameObject);

        if (bagged)
        {
            Debug.Log("Checkout item successfully bagged.");
        }
    }



    public void SetCheckoutCustomer(Customer customer)
    {
        checkoutCustomer = customer;

        if (checkoutCustomer != null)
        {
            Debug.Log(
                $"BagSpawn: {checkoutCustomer.name} is now checking out."
            );
        }
    }

    public void SpawnBag()
    {
        Debug.Log("BagSpawn: SpawnBag() called.");

        // A customer must be assigned before a bag can spawn.
        if (checkoutCustomer == null)
        {
            Debug.Log(
                "BagSpawn: Cannot spawn a bag because no customer is checking out."
            );
            return;
        }

        // Prevent spawning multiple bags for the same checkout.
        if (ActiveBag != null)
        {
            Debug.Log("BagSpawn: A bag already exists for this checkout.");
            return;
        }

        if (bagPrefab == null || bagSpawnPoint == null)
        {
            Debug.LogError(
                "BagSpawn: Assign the bag prefab and spawn point.",
                this
            );
            return;
        }

        GameObject bagObject = Instantiate(
            bagPrefab,
            bagSpawnPoint.position,
            bagSpawnPoint.rotation
        );

        BagContainer newBag = bagObject.GetComponent<BagContainer>();

        if (newBag == null)
        {
            Debug.LogError(
                "BagSpawn: The bag prefab needs a BagContainer component.",
                bagObject
            );

            Destroy(bagObject);
            return;
        }

        ActiveBag = newBag;

        // Assign the bag to the customer currently checking out.
        checkoutCustomer.AssignCheckoutBag(ActiveBag);

        Debug.Log(
            $"BagSpawn: Bag spawned and assigned to {checkoutCustomer.name}."
        );
    }


    public void ClearTakenBag(BagContainer takenBag)
    {
        if (ActiveBag == takenBag)
        {
            ActiveBag = null;
        }

        checkoutCustomer = null;
    }
}

