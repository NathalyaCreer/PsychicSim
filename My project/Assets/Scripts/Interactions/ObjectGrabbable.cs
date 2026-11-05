using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    
    private Rigidbody objectRigidbody;


    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
    }

    public void Grab(Transform grabPointTransform)
    {
        //set the parent of the object to the grab point and hold it there
        
        objectRigidbody.useGravity = false;
        objectRigidbody.isKinematic = true;
        objectRigidbody.linearVelocity = Vector3.zero;
        objectRigidbody.angularVelocity = Vector3.zero;

        transform.SetParent(grabPointTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);

        objectRigidbody.isKinematic = false;
        objectRigidbody.useGravity = true;
    }



}
