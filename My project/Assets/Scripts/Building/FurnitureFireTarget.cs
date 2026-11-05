using UnityEngine;

public class FurnitureFireTarget : MonoBehaviour
{
    [Header("Fire Placement")]
    [SerializeField] private Transform fireSpawnPoint;

    public bool HasBeenBurned { get; private set; }

    public Vector3 Position =>
        transform.position;

    public Vector3 FirePosition =>
        fireSpawnPoint != null
            ? fireSpawnPoint.position
            : transform.position;

    public void MarkBurned()
    {
        HasBeenBurned = true;
    }
}
