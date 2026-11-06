using UnityEngine;

public class Raycaster : MonoBehaviour
{
    
    [SerializeField] private Camera mainCam = null;

    private RaycastHit hit = new RaycastHit();


    public int GetHitLayer => hit.collider != null ? hit.collider.gameObject.layer : -1;
    public Vector3 GetHitPosition => hit.collider != null ? hit.point : Vector3.zero;




    void Update()
    {
        if(Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out hit, 100f))
        {
            print("Raycast hit: " + hit.collider.gameObject.name);
        }
    }
}
