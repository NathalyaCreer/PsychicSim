using UnityEngine;

public class BuildPlacer : MonoBehaviour
{
    [SerializeField] Raycaster raycaster = null;
    [SerializeField] MeshCollider meshCollider = null;
    [SerializeField] bool canBuild = false;
    [SerializeField] int buildLayer = 9;

    public bool CanBuild => canBuild;

    private void Update()
    {
        if(!raycaster)
        {
            return;
        }

        if(raycaster.GetHitLayer != buildLayer)
        {
            return;
        }

        transform.position = raycaster.GetHitPosition;
    }
}
