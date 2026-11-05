using UnityEngine;

public class DumpsterBodyDisposal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PossessedCustomer body =
            other.GetComponentInParent<PossessedCustomer>();

        if (body == null || !body.IsDeadBody)
            return;

        Debug.Log(
            $"{body.name}: Dead body disposed in dumpster.",
            body
        );

        body.MarkDeadBodyDisposed();
    }
}
