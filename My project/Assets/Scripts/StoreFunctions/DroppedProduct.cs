
using UnityEngine;

public class DroppedProduct : MonoBehaviour
{
    public ProductData ProductData { get; private set; }

    public void Initialize(ProductData productData)
    {
        ProductData = productData;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private bool TrySetLayerRecursively(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogError(
                $"DroppedProduct: Layer '{layerName}' doesn't exist. Create it in Project Settings > Tags and Layers.",
                this);
            return false;
        }

        SetLayerRecursively(gameObject, layer);
        return true;
    }

    // Called when the customer abandons the product.
    public void PrepareForPickup()
    {
        if (!TrySetLayerRecursively("DroppedProduct"))
            return;

        transform.SetParent(null, true);

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    // Called when the player returns the dropped item to a shelf.
    public void PrepareForShelf(Transform slot)
    {
        if (slot == null)
        {
            Debug.LogError("DroppedProduct: Shelf slot is missing.", this);
            return;
        }

        if (!TrySetLayerRecursively("ShelfProduct"))
            return;

        transform.SetParent(slot, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Use rb.velocity instead if your Unity version doesn't support linearVelocity.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }
}
