
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductShopCard : MonoBehaviour
{
    [Header("Card UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image productImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image qualityImage;

    [Header("Quality Images")]
    [SerializeField] private Sprite lowQualityImage;
    [SerializeField] private Sprite midQualityImage;
    [SerializeField] private Sprite highQualityImage;

    [Header("Quality Thresholds")]
    [SerializeField] private int midQualityThreshold = 40;
    [SerializeField] private int highQualityThreshold = 70;

    [Header("Purchase UI")]
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button buyButton;

    private ProductData product;
    private ProductCatalogPage catalogPage;


    private int Quantity
    {
        get
        {
            if (quantityInput != null &&
                int.TryParse(quantityInput.text, out int parsedQuantity))
            {
                return Mathf.Clamp(parsedQuantity, 1, 999);
            }

            return 1;
        }
    }

    public void Setup(ProductData productData, ProductCatalogPage page)
    {
        product = productData;
        catalogPage = page;

        if (product == null)
        {
            Debug.LogWarning("ProductShopCard received a null ProductData.");
            return;
        }

        if (titleText != null)
            titleText.text = product.productName;

        if (descriptionText != null)
            descriptionText.text = product.description;

        if (productImage != null)
            productImage.sprite = product.productImage;

        UpdateQualityImage(product.quality);

        if (quantityInput != null)
        {
            quantityInput.SetTextWithoutNotify("1");

            quantityInput.onValueChanged.RemoveListener(OnQuantityChanged);
            quantityInput.onValueChanged.AddListener(OnQuantityChanged);

            quantityInput.onEndEdit.RemoveListener(OnQuantityEndEdit);
            quantityInput.onEndEdit.AddListener(OnQuantityEndEdit);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }

        UpdatePriceDisplay();
    }

    private void UpdateQualityImage(int quality)
    {
        if (qualityImage == null)
        {
            Debug.LogWarning(
                $"Quality Image is not assigned on {gameObject.name}."
            );
            return;
        }

        if (quality >= highQualityThreshold)
        {
            qualityImage.sprite = highQualityImage;
        }
        else if (quality >= midQualityThreshold)
        {
            qualityImage.sprite = midQualityImage;
        }
        else
        {
            qualityImage.sprite = lowQualityImage;
        }

        qualityImage.enabled = qualityImage.sprite != null;
    }

    private void OnQuantityChanged(string value)
    {
        UpdatePriceDisplay();
    }

    private void OnQuantityEndEdit(string value)
    {
        int quantity = Quantity;

        if (quantityInput != null)
            quantityInput.SetTextWithoutNotify(quantity.ToString());

        UpdatePriceDisplay();
    }

    private void UpdatePriceDisplay()
    {
        if (product == null || priceText == null)
            return;

        long total = (long)product.purchasePrice * Quantity;
        priceText.text = $"${total:N0}";
    }

    public void Buy()
    {
        if (product == null || catalogPage == null)
        {
            Debug.LogWarning(
                "Product card is missing its product or catalog page reference."
            );
            return;
        }

        catalogPage.TryPurchaseProduct(product, Quantity);
    }
}
