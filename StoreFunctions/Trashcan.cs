
using UnityEngine;

public class Trashcan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Find a ProductBox on the object that touched the trigger.
        ProductBox box = other.GetComponentInParent<ProductBox>();

        if (box == null)
            return;

        // Only destroy boxes that are empty.
        if (box.amountInBox <= 0)
        {
            Destroy(box.gameObject);
        }
    }
}

