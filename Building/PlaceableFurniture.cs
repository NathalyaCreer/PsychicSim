using UnityEngine;

public class PlaceableFurniture : MonoBehaviour
{
    [Header("Placement Bounds")]
    [Tooltip("A BoxCollider defining the space this furniture occupies.")]
    [SerializeField] private BoxCollider placementBounds;

    public BoxCollider PlacementBounds => placementBounds;

    private void Reset()
    {
        placementBounds = GetComponent<BoxCollider>();

        if (placementBounds == null)
            placementBounds = GetComponentInChildren<BoxCollider>();
    }

    private void OnDrawGizmosSelected()
    {
        if (placementBounds == null)
            return;

        Gizmos.color = Color.cyan;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = placementBounds.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            placementBounds.center,
            placementBounds.size
        );

        Gizmos.matrix = oldMatrix;
    }
}