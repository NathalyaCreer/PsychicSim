
using UnityEngine;

public enum DeliveryProductType
{
    Stock,
    Furniture,
    Decor
}

[CreateAssetMenu(menuName = "Store/Product Data")]
public class ProductData : ScriptableObject
{
    [Header("Product Information")]
    public string productName;

    [TextArea(2, 4)]
    public string description;

    [Header("Product Stats")]
    [Range(0, 100)]
    public int quality = 50;

    public int effectiveness = 0;

    [Header("Pricing")]
    [Min(0)]
    public int purchasePrice = 1;

    [Min(0)]
    public int sellPrice = 2;

    [Header("Delivery")]
    public DeliveryProductType deliveryType = DeliveryProductType.Stock;

    [Tooltip("Prefab to place in the store for Furniture or Decor products.")]
    public GameObject placementPrefab;

    [Header("Visuals")]
    public Sprite productImage;

    [Tooltip("Individual product prefab spawned when opening a stock delivery box.")]
    public GameObject shelfPrefab;

    public bool IsHighQuality => quality >= 70;

    public bool IsPlaceable =>
        deliveryType == DeliveryProductType.Furniture ||
        deliveryType == DeliveryProductType.Decor;
}
