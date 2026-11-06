using UnityEngine;
using System.Collections.Generic;


// Keep this in BagSpawn.cs, below the BagSpawn class.
public class BagContainer : MonoBehaviour
{
    [SerializeField] private Transform bagInterior;
    [SerializeField] private Vector3 itemSpacing = new Vector3(0.12f, 0.08f, 0.12f);
    [SerializeField] private int itemsPerRow = 3;
    [SerializeField] private Transform[] itemPositions;

    private readonly HashSet<GameObject> baggedItems = new HashSet<GameObject>();

    public Transform Interior => bagInterior;
    public int BaggedItemCount => baggedItems.Count;
    private Customer checkoutCustomer;
    private int nextPositionIndex = 0;

    public bool ContainsItem(GameObject item)
    {
        return item != null && baggedItems.Contains(item);
    }

    public bool ContainsAll(List<GameObject> orderItems)
    {
        if (orderItems == null)
            return true;

        foreach (GameObject item in orderItems)
        {
            if (item != null && !baggedItems.Contains(item))
                return false;
        }

        return true;
    }

    public void AddItem(GameObject item)
    {
        if (item == null || bagInterior == null || baggedItems.Contains(item))
            return;

        int index = baggedItems.Count;
        int column = index % Mathf.Max(1, itemsPerRow);
        int row = index / Mathf.Max(1, itemsPerRow);

        item.transform.SetParent(bagInterior);
        item.transform.localPosition = new Vector3(
            column * itemSpacing.x,
            row * itemSpacing.y,
            0f
        );
        item.transform.localRotation = Quaternion.identity;

        foreach (Collider col in item.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        baggedItems.Add(item);
    }

    public void SetCheckoutCustomer(Customer customer)
    {
        checkoutCustomer = customer;
    }

    public Vector3 GetNextItemWorldPosition()
    {
        if (itemPositions == null || itemPositions.Length == 0)
        {
            Debug.LogError("BagContainer has no item positions assigned.", this);
            return transform.position;
        }

        int index = Mathf.Min(nextPositionIndex, itemPositions.Length - 1);
        nextPositionIndex++;

        return itemPositions[index].position;
    }
}
