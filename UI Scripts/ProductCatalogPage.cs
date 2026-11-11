using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProductCatalogPage : MonoBehaviour
{
    [Header("Product Catalog")]
    [SerializeField] private List<ProductData> products = new List<ProductData>();

    [Header("Grid UI")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private ProductShopCard productCardPrefab;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text messageText;

    // Temporary queue. We'll connect this to the delivery system later.

    private void OnEnable()
    {
        PopulateProducts();
        RefreshBalance();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
    }

    public void PopulateProducts()
    {
        if (gridContent == null || productCardPrefab == null)
        {
            Debug.LogError(
                "ProductCatalogPage: Assign Grid Content and Product Card Prefab.",
                this
            );
            return;
        }

        // Clear old cards before rebuilding the page.
        for (int i = gridContent.childCount - 1; i >= 0; i--)
            Destroy(gridContent.GetChild(i).gameObject);

        foreach (ProductData product in products)
        {
            if (product == null)
                continue;

            ProductShopCard card = Instantiate(productCardPrefab, gridContent);
            card.Setup(product, this);
        }
    }

    public void TryPurchaseProduct(ProductData product, int quantity)
    {
        if (product == null)
        {
            ShowMessage("Product is missing.");
            return;
        }

        if (quantity < 1)
        {
            ShowMessage("Choose a quantity of at least 1.");
            return;
        }

        // Use long for the calculation so a large order cannot overflow.
        long totalCost = (long)product.purchasePrice * quantity;

        // MoneyManager.TrySpendMoney takes an int.
        if (totalCost > int.MaxValue)
        {
            ShowMessage("That order is too large.");
            return;
        }

        int purchaseTotal = (int)totalCost;

        if (MoneyManager.Instance == null)
        {
            ShowMessage("Money manager is unavailable.");
            return;
        }

        if (DeliveryManager.Instance == null)
        {
            ShowMessage("Delivery manager is unavailable.");
            return;
        }

        if (MoneyManager.Instance.Balance < purchaseTotal)
        {
            ShowMessage($"Not enough money. Total: ${purchaseTotal}");
            return;
        }

        // Queue first; only charge if the delivery accepts the order.
        if (!DeliveryManager.Instance.QueuePurchase(product, quantity))
        {
            ShowMessage("Could not queue this delivery. Purchase not completed.");
            return;
        }

        if (!MoneyManager.Instance.TrySpendMoney(purchaseTotal))
        {
            ShowMessage("Payment failed.");
            return;
        }

        ShowMessage($"Purchased {quantity} x {product.productName} for ${purchaseTotal}.");
    }

    private void RefreshBalance()
    {
        if (balanceText != null && MoneyManager.Instance != null)
            balanceText.text = $"Money: ${MoneyManager.Instance.Balance}";
    }

    private void HandleBalanceChanged(int newBalance)
    {
        if (balanceText != null)
            balanceText.text = $"Money: ${newBalance}";
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

}
