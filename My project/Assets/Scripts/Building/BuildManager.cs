using UnityEngine;

public class BuildManager : MonoBehaviour
{
    [SerializeField] private Raycaster raycaster = null;
    [SerializeField] private BuildPlacer placer = null;


    [Space, Header("Conditions")]
    [SerializeField] private int buildLayer = 9;

    [Space, Header("Stuff to Build")]
    [SerializeField] private GameObject prefabToBuild;

    [Space, Header("Input")]
    [SerializeField] private KeyCode buildKey = KeyCode.Mouse0;

    bool canBuild => raycaster != null 
        && raycaster.GetHitLayer == buildLayer 
        && placer != null 
        && placer.CanBuild;

    

    void Update()
    {
        if (Input.GetKeyDown(buildKey) && raycaster && canBuild)
        {
            GameObject buildingObject = Instantiate(prefabToBuild, raycaster.GetHitPosition, Quaternion.identity);
        }
    }
}
