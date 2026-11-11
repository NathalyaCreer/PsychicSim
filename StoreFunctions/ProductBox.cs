
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

    public int OpenedFrame { get; private set; } = -1;

    private int configuredDeliveryQuantity = -1;

    private Renderer[] boxRenderers;
    private Collider[] boxColliders;
    private bool[] originalRendererStates;
    private bool[] originalColliderStates;

    public int BoxCapacity =>
        boxPositions == null ? 0 : boxPositions.Length;

    public void InitializeDelivery(ProductData data, int quantity)
    {
        productData = data;
        configuredDeliveryQuantity = Mathf.Clamp(quantity, 0, BoxCapacity);
        amountInBox = configuredDeliveryQuantity;
    }

    private void Awake()
    {
        CacheBoxComponents();
    }

    private void Start()
    {
        IsOpened = false;

        if (boxPositions == null)
            boxPositions = new Transform[0];

        if (productsInBox == null || productsInBox.Length != boxPositions.Length)
            productsInBox = new GameObject[boxPositions.Length];

        if (configuredDeliveryQuantity < 0)
            amountInBox = boxPositions.Length;
        else
            amountInBox = configuredDeliveryQuantity;
    }

    private void CacheBoxComponents()
    {
        boxRenderers = GetComponentsInChildren<Renderer>(true);
        boxColliders = GetComponentsInChildren<Collider>(true);

        originalRendererStates = new bool[boxRenderers.Length];
        originalColliderStates = new bool[boxColliders.Length];

        for (int i = 0; i < boxRenderers.Length; i++)
            originalRendererStates[i] = boxRenderers[i].enabled;

        for (int i = 0; i < boxColliders.Length; i++)
            originalColliderStates[i] = boxColliders[i].enabled;
    }

    public void OpenBox()
    {
        if (IsOpened)
            return;

        // Furniture and decor use placement mode instead of spawning stock.
        if (productData != null && productData.IsPlaceable)
        {
            if (productData.placementPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: This placeable product has no Placement Prefab assigned.",
                    this
                );
                return;
            }

            if (FurniturePlacementManager.Instance == null)
            {
                Debug.LogError(
                    "No FurniturePlacementManager exists in the scene.",
                    this
                );
                return;
            }
            Debug.Log(
                $"Opening placeable box: {name}. " +
                $"Product: {productData.productName}. " +
                $"Placement prefab: {productData.placementPrefab.name}"
            );

            bool started = FurniturePlacementManager.Instance.BeginPlacement(
                productData.placementPrefab,
                this
            );

            Debug.Log($"Furniture placement started: {started}");

            if (started)
                OpenedFrame = Time.frameCount;

            return;
        }

        // Existing stock-box behavior.
        OpenStockBox();
    }

    private void OpenStockBox()
    {
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
            Debug.LogWarning(
                $"{name}: Assign a product prefab to spawn inside the box.",
                this
            );
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

            BoxedProductItem boxedItem =
                item.GetComponent<BoxedProductItem>();

            if (boxedItem == null)
                boxedItem = item.AddComponent<BoxedProductItem>();

            boxedItem.Initialize(this, item);
        }

        Debug.Log(
            $"Opened {productData?.productName ?? name} box ({amountInBox} items)."
        );
    }

    // Called when placement begins. The box is hidden but not destroyed.
    public void HideForPlacement()
    {
        SetBoxVisible(false);
    }

    // Called if the player cancels placement.
    public void RestoreAfterPlacement()
    {
        SetBoxVisible(true);
    }

    // Called only after the furniture has been successfully placed.
    public void ConsumeAfterPlacement()
    {
        IsOpened = true;
        OpenedFrame = Time.frameCount;
        Destroy(gameObject);
    }

    private void SetBoxVisible(bool visible)
    {
        if (boxRenderers == null || boxColliders == null)
            CacheBoxComponents();

        for (int i = 0; i < boxRenderers.Length; i++)
        {
            if (boxRenderers[i] != null)
                boxRenderers[i].enabled =
                    visible && originalRendererStates[i];
        }

        for (int i = 0; i < boxColliders.Length; i++)
        {
            if (boxColliders[i] != null)
                boxColliders[i].enabled =
                    visible && originalColliderStates[i];
        }
    }

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
