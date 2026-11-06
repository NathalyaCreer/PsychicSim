
using UnityEngine;

public class ReStockShelf : MonoBehaviour
{
    [Header("Shelf")]
    public Transform[] shelfPositions;
    public GameObject productPrefab;

    private GameObject[] shelfProducts;
    private int amountOnShelf = 0;
    private ProductData currentProductType;

    private int shelfFull => shelfPositions != null
        ? shelfPositions.Length
        : 0;

    [Header("Restocking")]
    public float restockInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float restockHoldDuration = 0.6f;

    [SerializeField] private PlayerPickupDrop playerPickupDrop;

    private ProductBox productBox;
    private bool playerInRange;
    private float restockHoldTimer;

    public ProductData CurrentProductType => currentProductType;
    public int AmountOnShelf => amountOnShelf;
    public bool IsFull => amountOnShelf >= shelfFull;

    private void Start()
    {
        shelfProducts = new GameObject[shelfFull];

        // Register products already placed in shelf slots in the Inspector.
        for (int i = 0; i < shelfFull; i++)
        {
            if (shelfPositions[i] == null || shelfPositions[i].childCount == 0)
                continue;

            shelfProducts[i] = shelfPositions[i].GetChild(0).gameObject;
            amountOnShelf++;

            DroppedProduct dropped =
                shelfProducts[i].GetComponent<DroppedProduct>();

            if (currentProductType == null && dropped != null)
                currentProductType = dropped.ProductData;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || playerPickupDrop == null)
        {
            restockHoldTimer = 0f;
            return;
        }

        // Releasing Q cancels the current restock hold.
        if (!Input.GetKey(KeyCode.Q))
        {
            restockHoldTimer = 0f;
            return;
        }

        ObjectGrabbable heldObject = playerPickupDrop.HeldObject;

        if (heldObject == null)
        {
            restockHoldTimer = 0f;
            return;
        }

        ProductBox heldBox = heldObject.GetComponent<ProductBox>();
        DroppedProduct droppedProduct = heldObject.GetComponent<DroppedProduct>();

        // A sealed box must be opened first. PlayerPickupDrop handles
        // opening it on Q-down; don't restock it during that same frame.
        if (heldBox != null)
        {
            if (!heldBox.IsOpened || heldBox.OpenedFrame == Time.frameCount)
            {
                restockHoldTimer = 0f;
                return;
            }

            if (heldBox.amountInBox <= 0)
            {
                restockHoldTimer = 0f;
                return;
            }

            productBox = heldBox;
        }
        else if (droppedProduct == null)
        {
            restockHoldTimer = 0f;
            return;
        }

        restockHoldTimer += Time.deltaTime;

        if (restockHoldTimer < restockHoldDuration)
            return;

        // One completed hold restocks one unit.
        restockHoldTimer = 0f;

        if (heldBox != null)
            RestockProduct();
        else
            RestockDroppedProduct(droppedProduct);
    }

    private int FindEmptySlot()
    {
        if (shelfPositions == null || shelfProducts == null)
            return -1;

        for (int i = 0; i < shelfPositions.Length; i++)
        {
            if (shelfPositions[i] != null && shelfProducts[i] == null)
                return i;
        }

        return -1;
    }

    private bool CanAcceptProduct(ProductData incomingType)
    {
        if (incomingType == null || IsFull)
            return false;

        // A shelf can only contain one product type at a time.
        return currentProductType == null ||
               currentProductType == incomingType;
    }

    private void RestockDroppedProduct(DroppedProduct droppedProduct)
    {
        if (droppedProduct == null)
            return;

        ProductData incomingType = droppedProduct.ProductData;

        if (!CanAcceptProduct(incomingType))
        {
            Debug.Log("This shelf cannot accept that product type.");
            return;
        }

        int slotIndex = FindEmptySlot();

        if (slotIndex < 0)
        {
            Debug.Log("This shelf is full.");
            return;
        }

        Transform slot = shelfPositions[slotIndex];

        // Preserve the existing product object; do not instantiate a duplicate.
        currentProductType = incomingType;
        droppedProduct.PrepareForShelf(slot);

        shelfProducts[slotIndex] = droppedProduct.gameObject;
        amountOnShelf++;

        // Clear the player's held reference without dropping the item again.
        playerPickupDrop.ForgetHeldObject(droppedProduct.GetComponent<ObjectGrabbable>());

        Debug.Log($"Restocked {incomingType.productName}.");
    }

    private void RestockProduct()
    {
        
        if (productBox == null || !productBox.IsOpened)
        {
            Debug.Log("Open the product box before restocking.");
            return;
        }

        if(productBox.amountInBox <= 0)
            return;

        if (productBox == null || productBox.amountInBox <= 0)
            return;

        ProductData incomingType = productBox.ProductType;

        if (!CanAcceptProduct(incomingType))
            return;

        int slotIndex = FindEmptySlot();

        if (slotIndex < 0)
            return;

        Transform slot = shelfPositions[slotIndex];

        GameObject prefab = incomingType.shelfPrefab;

        if (prefab == null)
        {
            Debug.LogWarning(
                $"No shelf prefab assigned for {incomingType.productName}."
            );
            return;
        }

        GameObject newProduct = Instantiate(
            prefab,
            slot.position,
            slot.rotation,
            slot
        );

        // Only consume box inventory after successfully creating the product.
        currentProductType = incomingType;
        shelfProducts[slotIndex] = newProduct;
        amountOnShelf++;
        productBox.RemoveProduct();
    }

    public GameObject TakeProduct()
    {
        if (amountOnShelf <= 0 || shelfProducts == null)
            return null;

        for (int i = 0; i < shelfProducts.Length; i++)
        {
            if (shelfProducts[i] == null)
                continue;

            GameObject product = shelfProducts[i];

            shelfProducts[i] = null;
            amountOnShelf--;

            if (amountOnShelf == 0)
                currentProductType = null;

            return product;
        }

        return null;
    }

    public void DestroyAllProductsForFire()
    {
        if (shelfProducts == null)
            return;

        for (int i = 0; i < shelfProducts.Length; i++)
        {
            if (shelfProducts[i] != null)
            {
                Destroy(shelfProducts[i]);
                shelfProducts[i] = null;
            }
        }

        amountOnShelf = 0;
        currentProductType = null;

        Debug.Log($"{name}: All stored products were destroyed by fire.", this);
    }
}
