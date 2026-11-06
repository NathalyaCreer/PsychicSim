using UnityEngine;

public class StoreExitBoundary : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PossessedCustomer customer =
            other.GetComponentInParent<PossessedCustomer>();

        if (customer == null)
            return;

        if (customer.IsPossessed && customer.WasThrown)
        {
            customer.ThrowOutOfStore();
        }
    }
}

