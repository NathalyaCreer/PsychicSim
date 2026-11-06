
using UnityEngine;

public class FurnitureFireTarget : MonoBehaviour
{
    [Header("Fire Placement")]
    [SerializeField] private Transform fireSpawnPoint;

    [Header("Burnt Appearance")]
    [SerializeField] private Material burntMaterial;

    [Header("Repair")]
    [SerializeField, Min(0)] private int repairCost = 25;
    [SerializeField, Min(1)] private int repairHitsRequired = 3;

    public bool HasBeenBurned { get; private set; }
    public bool IsBurnt => HasBeenBurned;

    public Vector3 Position => transform.position;

    public Vector3 FirePosition =>
        fireSpawnPoint != null
            ? fireSpawnPoint.position
            : transform.position;

    public int RepairCost => repairCost;
    public int RepairHitsRequired => repairHitsRequired;
    public int RepairHitsRemaining =>
        Mathf.Max(0, repairHitsRequired - repairHits);

    private Renderer[] furnitureRenderers;
    private Material[][] originalMaterials;
    private int repairHits;

    private void Awake()
    {
        furnitureRenderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[furnitureRenderers.Length][];

        for (int i = 0; i < furnitureRenderers.Length; i++)
        {
            originalMaterials[i] =
                furnitureRenderers[i].sharedMaterials;
        }
    }

    public void MarkBurned()
    {
        if (HasBeenBurned)
            return;

        HasBeenBurned = true;
        repairHits = 0;

        ApplyBurntMaterial();
        DestroyStoredProducts();

        Debug.Log($"{name} has been burnt.", this);
    }

    private void ApplyBurntMaterial()
    {
        if (burntMaterial == null)
        {
            Debug.LogWarning(
                $"{name}: No burnt material assigned.",
                this
            );

            return;
        }

        foreach (Renderer furnitureRenderer in furnitureRenderers)
        {
            if (furnitureRenderer == null)
                continue;

            Material[] materials =
                furnitureRenderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = burntMaterial;

            furnitureRenderer.sharedMaterials = materials;
        }
    }

    private void DestroyStoredProducts()
    {
        // Destroy products held by shelves that are children of this furniture.
        ReStockShelf[] shelves =
            GetComponentsInChildren<ReStockShelf>(true);

        foreach (ReStockShelf shelf in shelves)
        {
            if (shelf != null)
                shelf.DestroyAllProductsForFire();
        }
    }

    public bool TryRepairHit()
    {
        if (!HasBeenBurned)
            return false;

        repairHits++;

        Debug.Log(
            $"{name}: Repair hit {repairHits}/{repairHitsRequired}.",
            this
        );

        if (repairHits < repairHitsRequired)
            return false;

        MoneyManager moneyManager = MoneyManager.Instance;

        if (moneyManager == null)
        {
            Debug.LogWarning(
                "Furniture repair failed: MoneyManager.Instance is missing.",
                this
            );

            repairHits = repairHitsRequired - 1;
            return false;
        }

        if (!moneyManager.TrySpendMoney(repairCost))
        {
            Debug.Log(
                $"Not enough money to repair {name}. Repair cost: ${repairCost}.",
                this
            );

            // Allow the player to try again after earning more money.
            repairHits = repairHitsRequired - 1;
            return false;
        }

        RestoreFurniture();

        return true;
    }

    private void RestoreFurniture()
    {
        for (int i = 0; i < furnitureRenderers.Length; i++)
        {
            if (furnitureRenderers[i] == null)
                continue;

            furnitureRenderers[i].sharedMaterials =
                originalMaterials[i];
        }

        HasBeenBurned = false;
        repairHits = 0;

        Debug.Log($"{name} has been repaired.", this);
    }
}
