using System.Collections.Generic;
using UnityEngine;

public class QueueLine : MonoBehaviour
{
    [Header("Queue Layout")]
    [SerializeField] private Transform frontPosition;
    [SerializeField] private Vector3 queueDirection = Vector3.back;
    [SerializeField] private float spacing = 1.25f;

    private readonly List<Customer> customers = new List<Customer>();

    public void JoinQueue(Customer customer)
    {
        if (customer == null || customers.Contains(customer))
            return;

        customers.Add(customer);
        UpdateQueuePositions();
    }

    public void LeaveQueue(Customer customer)
    {
        if (customer == null)
            return;

        customers.Remove(customer);
        UpdateQueuePositions();
    }

    public bool IsAtFront(Customer customer)
    {
        return customers.Count > 0 && customers[0] == customer;
    }

    public bool IsReadyForCheckout(Customer customer)
    {
        if (customer == null || frontPosition == null)
        {
            Debug.LogError("QueueLine: Customer or front position is missing.", this);
            return false;
        }

        bool isFront = IsAtFront(customer);
        bool isNearFront = customer.IsNearQueuePosition(frontPosition.position);

        return isFront && isNearFront;
    }

    private void Update()
    {
        // Remove customers that were destroyed or disabled.
        customers.RemoveAll(customer => customer == null);

        UpdateQueuePositions();
    }

    private void UpdateQueuePositions()
    {
        if (frontPosition == null)
            return;

        Vector3 direction = queueDirection.normalized;

        for (int i = 0; i < customers.Count; i++)
        {
            Vector3 targetPosition =
                frontPosition.position - direction * spacing * i;

            customers[i].MoveToQueuePosition(targetPosition);
        }
    }
}
