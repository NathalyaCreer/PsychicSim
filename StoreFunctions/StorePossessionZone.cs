using UnityEngine;

public class StorePossessionZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PossessedCustomer possessed =
            other.GetComponentInParent<PossessedCustomer>();

        if (possessed == null)
            return;

        possessed.SetInsideStore(true);

        Debug.Log($"{possessed.name} entered the store.");
    }

    private void OnTriggerExit(Collider other)
    {
        PossessedCustomer possessed =
            other.GetComponentInParent<PossessedCustomer>();

        if (possessed == null)
            return;

        possessed.SetInsideStore(false);

        Debug.Log($"{possessed.name} left the store.");
    }
}

