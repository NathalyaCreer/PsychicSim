using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObjectGrabbable : MonoBehaviour
{
    private Rigidbody objectRigidbody;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
    }

    public void Grab(Transform grabPointTransform)
    {
        if (grabPointTransform == null)
        {
            Debug.LogWarning($"{name}: No grab point assigned.", this);
            return;
        }

        if (objectRigidbody == null)
            objectRigidbody = GetComponent<Rigidbody>();

        if (objectRigidbody == null)
        {
            Debug.LogError($"{name}: Missing Rigidbody.", this);
            return;
        }

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
        if (objectRigidbody == null)
            objectRigidbody = GetComponent<Rigidbody>();

        transform.SetParent(null);

        if (objectRigidbody == null)
            return;

        objectRigidbody.isKinematic = false;
        objectRigidbody.useGravity = true;
    }
}
