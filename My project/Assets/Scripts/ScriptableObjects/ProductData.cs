using UnityEngine;

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

    [Header("Visuals")]
    public Sprite productImage;
    public GameObject shelfPrefab;

    // Products with quality 70 or higher count as high quality.
    public bool IsHighQuality => quality >= 70;
}