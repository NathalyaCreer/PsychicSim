using UnityEngine;

public class FurnitureFireTarget : MonoBehaviour
{
    [Header("Fire Placement")]
    [SerializeField] private Transform fireSpawnPoint;

    [Header("Burnt Appearance")]
    [SerializeField] private Material burntMaterial;

    public bool HasBeenBurned { get; private set; }
    public bool IsBurnt => HasBeenBurned;

    public Vector3 Position => transform.position;

    public Vector3 FirePosition =>
        fireSpawnPoint != null
            ? fireSpawnPoint.position
            : transform.position;

    private FixableFurniture fixableFurniture;
    private Renderer[] furnitureRenderers;
    private Material[][] originalMaterials;

    private void Awake()
    {
        fixableFurniture = GetComponent<FixableFurniture>();

        if (fixableFurniture == null)
        {
            Debug.LogWarning(
                $"{name} has FurnitureFireTarget but no FixableFurniture component.",
                this
            );
        }
        else
        {
            fixableFurniture.OnRepaired += RestoreFurniture;
        }

        furnitureRenderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[furnitureRenderers.Length][];

        for (int i = 0; i < furnitureRenderers.Length; i++)
        {
            originalMaterials[i] =
                furnitureRenderers[i].sharedMaterials;
        }
    }

    private void OnDestroy()
    {
        if (fixableFurniture != null)
            fixableFurniture.OnRepaired -= RestoreFurniture;
    }

    public void MarkBurned()
    {
        if (HasBeenBurned)
            return;

        HasBeenBurned = true;

        if (fixableFurniture != null)
            fixableFurniture.SetBroken();

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
        ReStockShelf[] shelves =
            GetComponentsInChildren<ReStockShelf>(true);

        foreach (ReStockShelf shelf in shelves)
        {
            if (shelf != null)
                shelf.DestroyAllProductsForFire();
        }
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

        Debug.Log($"{name} has been restored after repair.", this);
    }
}