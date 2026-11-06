using UnityEngine;

public class ProductBox : MonoBehaviour
{
    [Header("Box Contents")]
    [SerializeField] private Transform[] boxPositions;
    [SerializeField] private GameObject boxedProductPrefab;
    [SerializeField] private ProductData productData;

    [Min(0)] public int amountInBox = 10;

    public GameObject[] productsInBox;

    public ProductData ProductType => productData;
    public bool IsOpened { get; private set; }

    // Used to stop Q opening a box and restocking it in the same frame.
    public int OpenedFrame { get; private set; } = -1;

    private int configuredDeliveryQuantity = -1;

    public int BoxCapacity =>
        boxPositions == null ? 0 : boxPositions.Length;

    // Called by DeliveryManager immediately after spawning a delivery box.
    public void InitializeDelivery(ProductData data, int quantity)
    {
        productData = data;
        configuredDeliveryQuantity = Mathf.Clamp(quantity, 0, BoxCapacity);
        amountInBox = configuredDeliveryQuantity;
    }

    private void Start()
    {
        IsOpened = false;

        if (boxPositions == null)
            boxPositions = new Transform[0];

        if (productsInBox == null || productsInBox.Length != boxPositions.Length)
            productsInBox = new GameObject[boxPositions.Length];

        // If this is a delivery box, keep its specified partial quantity.
        if (configuredDeliveryQuantity < 0)
            amountInBox = boxPositions.Length;
        else
            amountInBox = configuredDeliveryQuantity;
    }

    public void OpenBox()
    {
        if (IsOpened)
            return;

        if (boxPositions == null || boxPositions.Length == 0)
        {
            Debug.LogWarning($"{name}: No box positions assigned.", this);
            return;
        }

        GameObject contentsPrefab =
            productData != null && productData.shelfPrefab != null
                ? productData.shelfPrefab
                : boxedProductPrefab;

        if (contentsPrefab == null)
        {
            Debug.LogWarning($"{name}: Assign a product prefab to spawn inside the box.", this);
            return;
        }

        IsOpened = true;
        OpenedFrame = Time.frameCount;

        int quantityToSpawn = Mathf.Clamp(amountInBox, 0, boxPositions.Length);

        for (int i = 0; i < quantityToSpawn; i++)
        {
            Transform slot = boxPositions[i];

            if (slot == null)
                continue;

            GameObject item = Instantiate(
                contentsPrefab,
                slot.position,
                slot.rotation,
                slot
            );

            productsInBox[i] = item;

            // Allows PlayerPickupDrop to register when a contained item
            // is taken out of this box.
            BoxedProductItem boxedItem = item.GetComponent<BoxedProductItem>();

            if (boxedItem == null)
                boxedItem = item.AddComponent<BoxedProductItem>();

            boxedItem.Initialize(this, item);
        }

        Debug.Log($"Opened {productData?.productName ?? name} box ({amountInBox} items).");
    }

    // Used when shelf restocking consumes one item.
    public void RemoveProduct()
    {
        if (!IsOpened || amountInBox <= 0)
            return;

        for (int i = productsInBox.Length - 1; i >= 0; i--)
        {
            if (productsInBox[i] == null)
                continue;

            Destroy(productsInBox[i]);
            productsInBox[i] = null;
            amountInBox--;
            break;
        }

        UpdateEmptyBoxLayer();
    }

    // Used when the player takes an individual item out of the box.
    public void RemoveContainedProduct(GameObject item)
    {
        if (!IsOpened || item == null)
            return;

        for (int i = 0; i < productsInBox.Length; i++)
        {
            if (productsInBox[i] != item)
                continue;

            productsInBox[i] = null;
            amountInBox = Mathf.Max(0, amountInBox - 1);
            item.transform.SetParent(null, true);

            UpdateEmptyBoxLayer();
            return;
        }
    }

    private void UpdateEmptyBoxLayer()
    {
        if (amountInBox > 0)
            return;

        int objectLayer = LayerMask.NameToLayer("Object");

        if (objectLayer != -1)
            gameObject.layer = objectLayer;
    }
}