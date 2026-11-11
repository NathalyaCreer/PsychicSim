using UnityEngine;

public class FurniturePlacementManager : MonoBehaviour
{
    public static FurniturePlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private PlayerPickupDrop playerPickupDrop;

    [Header("Placement Raycast")]
    [SerializeField] private LayerMask placementSurfaceLayers;
    [SerializeField] private LayerMask blockedLayers;
    [SerializeField] private float placementDistance = 8f;

    [Header("Placement")]
    [SerializeField] private float rotationStep = 90f;
    [SerializeField] private float surfaceOffset = 0f;

    [Header("Preview")]
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.15f, 0.15f, 0.45f);

    public bool IsPlacementActive => mode != PlacementMode.None;

    private enum PlacementMode
    {
        None,
        NewFurniture,
        MovingFurniture
    }

    private PlacementMode mode = PlacementMode.None;

    private GameObject furniturePrefab;
    private GameObject previewObject;
    private PlaceableFurniture previewFurniture;
    private ProductBox sourceBox;

    private PlaceableFurniture movingFurniture;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Rigidbody movingRigidbody;
    private bool originalKinematic;
    private bool originalUseGravity;
    private Collider[] movingColliders;
    private bool[] movingColliderStates;

    private int originalBoxSlot = -1;
    private bool isValidPlacement;
    private float currentRotation;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private static readonly int LegacyColor = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!IsPlacementActive)
            return;

        UpdatePreviewPosition();
    }

    public bool BeginPlacement(GameObject prefab, ProductBox box)
    {
        Debug.Log(
            $"FurniturePlacementManager.BeginPlacement called. " +
            $"Prefab: {(prefab != null ? prefab.name : "NULL")}, " +
            $"Box: {(box != null ? box.name : "NULL")}, " +
            $"Already active: {IsPlacementActive}"
        );

        if (IsPlacementActive || prefab == null || box == null)
        {
            Debug.LogWarning(
                "BeginPlacement stopped: placement is active, " +
                "or prefab/box is missing."
            );

            return false;
        }

        if (playerCamera == null || playerPickupDrop == null)
        {
            Debug.LogError(
                "BeginPlacement stopped: playerCamera or playerPickupDrop " +
                "is not assigned."
            );

            return false;
        }

        GameObject visualPrefab = previewPrefab != null
            ? previewPrefab
            : prefab;

        Debug.Log($"Creating placement preview from: {visualPrefab.name}");

        GameObject newPreview = Instantiate(visualPrefab);

        PlaceableFurniture placeable =
            newPreview.GetComponentInChildren<PlaceableFurniture>();

        if (placeable == null || placeable.PlacementBounds == null)
        {
            Debug.LogError(
                $"{visualPrefab.name} needs PlaceableFurniture " +
                "and a valid PlacementBounds BoxCollider."
            );

            Destroy(newPreview);
            return false;
        }

        Debug.Log("Preview has PlaceableFurniture and bounds.");

        if (!playerPickupDrop.BeginBoxPlacement(box, out int slot))
        {
            Debug.LogError(
                "BeginPlacement stopped: PlayerPickupDrop.BeginBoxPlacement returned false."
            );

            Destroy(newPreview);
            return false;
        }

        Debug.Log($"Box temporarily removed from hotbar slot {slot}.");

        box.HideForPlacement();

        furniturePrefab = prefab;
        sourceBox = box;
        originalBoxSlot = slot;

        previewObject = newPreview;
        previewFurniture = placeable;

        DisablePreviewGameplay();

        currentRotation = 0f;
        mode = PlacementMode.NewFurniture;

        Debug.Log("Furniture placement mode successfully activated.");

        return true;
    }

    public bool BeginMovingFurniture(PlaceableFurniture furniture)
    {
        if (IsPlacementActive || furniture == null ||
            furniture.PlacementBounds == null)
            return false;

        movingFurniture = furniture;

        originalPosition = furniture.transform.position;
        originalRotation = furniture.transform.rotation;
        originalParent = furniture.transform.parent;

        movingRigidbody = furniture.GetComponent<Rigidbody>();

        if (movingRigidbody != null)
        {
            originalKinematic = movingRigidbody.isKinematic;
            originalUseGravity = movingRigidbody.useGravity;

            movingRigidbody.isKinematic = true;
            movingRigidbody.useGravity = false;
        }

        movingColliders = furniture.GetComponentsInChildren<Collider>();
        movingColliderStates = new bool[movingColliders.Length];

        for (int i = 0; i < movingColliders.Length; i++)
        {
            movingColliderStates[i] = movingColliders[i].enabled;
            movingColliders[i].enabled = false;
        }

        previewObject = furniture.gameObject;
        previewFurniture = furniture;

        currentRotation = furniture.transform.eulerAngles.y;
        mode = PlacementMode.MovingFurniture;

        return true;
    }

    private void DisablePreviewGameplay()
    {
        // The preview is only a visual guide. It must not run gameplay scripts
        // or interfere with physics while the player is positioning it.
        MonoBehaviour[] scripts =
            previewObject.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != previewFurniture)
                script.enabled = false;
        }

        Collider[] colliders =
            previewObject.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
            col.enabled = false;

        Rigidbody[] rigidbodies =
            previewObject.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void UpdatePreviewPosition()
    {
        Debug.Log("PREVIEW 1: Starting update.");

        if (previewObject == null || playerCamera == null)
        {
            Debug.LogWarning(
                $"PREVIEW STOPPED: Preview missing: {previewObject == null}, " +
                $"Camera missing: {playerCamera == null}"
            );
            return;
        }

        Debug.Log("PREVIEW 2: References valid.");

        Ray ray = new Ray(
            playerCamera.position,
            playerCamera.forward
        );

        Vector3 targetPosition = ray.GetPoint(placementDistance);

        Debug.Log("PREVIEW 3: Ray created.");

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            placementDistance,
            placementSurfaceLayers,
            QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point + hit.normal * surfaceOffset;
        }

        Debug.Log("PREVIEW 4: Raycast completed.");

        previewObject.transform.position = targetPosition;

        previewObject.transform.rotation =
            Quaternion.Euler(0f, currentRotation, 0f);

        Debug.Log("PREVIEW 5: Transform updated.");

        isValidPlacement = CheckPlacementValidity();

        Debug.Log("PREVIEW 6: Placement validation completed.");

        UpdatePreviewColor();

        Debug.Log("PREVIEW 7: Preview update finished.");
    }

    private bool CheckPlacementValidity()
    {
        if (previewFurniture == null ||
            previewFurniture.PlacementBounds == null)
            return false;

        BoxCollider bounds = previewFurniture.PlacementBounds;

        Vector3 center =
            bounds.transform.TransformPoint(bounds.center);

        Vector3 halfExtents = Vector3.Scale(
            bounds.size * 0.5f,
            bounds.transform.lossyScale
        );

        halfExtents = new Vector3(
            Mathf.Abs(halfExtents.x),
            Mathf.Abs(halfExtents.y),
            Mathf.Abs(halfExtents.z)
        );

        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            bounds.transform.rotation,
            blockedLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null)
                continue;

            if (overlap.transform == previewObject.transform ||
                overlap.transform.IsChildOf(previewObject.transform))
                continue;

            Debug.LogWarning(
                $"Placement blocked by: {overlap.name} | " +
                $"Layer: {LayerMask.LayerToName(overlap.gameObject.layer)}"
            );

            return false;
        }

        return true;
    }

    private void UpdatePreviewColor()
    {
        Debug.Log("COLOR 1: Starting color update.");

        if (mode != PlacementMode.NewFurniture || previewObject == null)
        {
            Debug.Log("COLOR STOPPED: Not new furniture or preview missing.");
            return;
        }

        Color color = isValidPlacement ? validColor : invalidColor;

        Debug.Log("COLOR 2: Getting renderers.");

        Renderer[] renderers =
            previewObject.GetComponentsInChildren<Renderer>();

        Debug.Log($"COLOR 3: Found {renderers.Length} renderers.");

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Debug.Log("COLOR 4: Updating " + renderer.name);

            renderer.GetPropertyBlock(propertyBlock);
            Debug.Log("COLOR 4A: GetPropertyBlock successful.");

            propertyBlock.SetColor(BaseColor, color);
            Debug.Log("COLOR 4B: BaseColor successful.");

            propertyBlock.SetColor(LegacyColor, color);
            Debug.Log("COLOR 4C: LegacyColor successful.");

            renderer.SetPropertyBlock(propertyBlock);
            Debug.Log("COLOR 4D: SetPropertyBlock successful.");
        }

        Debug.Log("COLOR 5: Color update completed.");

        Debug.Log("COLOR 5: Color update completed.");
    }

    private void RotatePreview()
    {
        currentRotation += rotationStep;

        if (currentRotation >= 360f)
            currentRotation -= 360f;
    }

    private void ConfirmPlacement()
    {
        Debug.Log(
            $"ConfirmPlacement called. " +
            $"Valid: {isValidPlacement}, " +
            $"Preview: {previewObject != null}"
        );

        if (!isValidPlacement || previewObject == null)
        {
            Debug.Log("Placement blocked: preview is invalid or missing.");
            return;
        }

        if (mode == PlacementMode.NewFurniture)
        {
            Vector3 position = previewObject.transform.position;
            Quaternion rotation = previewObject.transform.rotation;

            Destroy(previewObject);
            previewObject = null;

            Instantiate(furniturePrefab, position, rotation);

            if (sourceBox != null)
                sourceBox.ConsumeAfterPlacement();

            playerPickupDrop.CompleteBoxPlacement();

            ClearPlacementState();
        }
        else if (mode == PlacementMode.MovingFurniture)
        {
            RestoreMovingFurnitureComponents();

            ClearPlacementState();
        }
    }

    public void CancelPlacement()
    {
        if (!IsPlacementActive)
            return;

        if (mode == PlacementMode.NewFurniture)
        {
            if (previewObject != null)
                Destroy(previewObject);

            if (sourceBox != null)
                sourceBox.RestoreAfterPlacement();

            if (playerPickupDrop != null)
            {
                playerPickupDrop.CancelBoxPlacement(
                    sourceBox,
                    originalBoxSlot
                );
            }
        }
        else if (mode == PlacementMode.MovingFurniture)
        {
            if (movingFurniture != null)
            {
                movingFurniture.transform.SetParent(originalParent, true);
                movingFurniture.transform.SetPositionAndRotation(
                    originalPosition,
                    originalRotation
                );

                RestoreMovingFurnitureComponents();
            }
        }

        ClearPlacementState();
    }

    private void RestoreMovingFurnitureComponents()
    {
        if (movingColliders != null)
        {
            for (int i = 0; i < movingColliders.Length; i++)
            {
                if (movingColliders[i] != null)
                    movingColliders[i].enabled = movingColliderStates[i];
            }
        }

        if (movingRigidbody != null)
        {
            movingRigidbody.isKinematic = originalKinematic;
            movingRigidbody.useGravity = originalUseGravity;
        }
    }

    private void ClearPlacementState()
    {
        mode = PlacementMode.None;

        furniturePrefab = null;
        previewObject = null;
        previewFurniture = null;
        sourceBox = null;
        originalBoxSlot = -1;

        movingFurniture = null;
        movingRigidbody = null;
        movingColliders = null;
        movingColliderStates = null;

        isValidPlacement = false;
        currentRotation = 0f;
    }

    public void RotatePlacement()
    {
        if (!IsPlacementActive)
            return;

        RotatePreview();
    }

    public void ConfirmPlacementInput()
    {
        Debug.Log("STEP 1: Confirm input received.");

        if (!IsPlacementActive)
        {
            Debug.LogWarning("STEP 2: Placement mode is inactive.");
            return;
        }

        Debug.Log("STEP 2: Updating preview position.");

        UpdatePreviewPosition();

        Debug.Log("STEP 3: Preview position updated.");

        Debug.Log(
            $"STEP 4: Valid: {isValidPlacement}, " +
            $"Preview exists: {previewObject != null}, " +
            $"Mode: {mode}"
        );

        ConfirmPlacement();

        Debug.Log("STEP 5: ConfirmPlacement finished.");

        Debug.Log($"STEP 6: Placement active: {IsPlacementActive}");
    }

    public void CancelPlacementInput()
    {
        if (!IsPlacementActive)
            return;

        CancelPlacement();
    }


}
