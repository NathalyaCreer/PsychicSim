
using System.Collections.Generic;
using UnityEngine;

public class FortuneTellingLine : MonoBehaviour
{
    [Header("Line Layout")]
    [SerializeField] private Transform frontPosition;
    [SerializeField] private Vector3 lineDirection = Vector3.back;
    [SerializeField] private float spacing = 1.25f;

    private readonly List<Customer> customers = new List<Customer>();

    public Customer FrontCustomer =>
        customers.Count > 0 ? customers[0] : null;

    public Transform FrontPosition => frontPosition;

    public int QueueCount => customers.Count;

    public void JoinLine(Customer customer)
    {
        if (customer == null || customers.Contains(customer))
            return;

        customers.Add(customer);
        UpdateQueuePositions();
    }

    public void LeaveLine(Customer customer)
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

    // Use this for the card-reading table.
    public bool IsReadyForReading(Customer customer)
    {
        if (customer == null || frontPosition == null)
            return false;

        return IsAtFront(customer) &&
               customer.IsNearQueuePosition(frontPosition.position);
    }

    // Retain this if another script uses your original checkout method.
    public bool IsReadyForCheckout(Customer customer)
    {
        return IsReadyForReading(customer);
    }

    private void Update()
    {
        customers.RemoveAll(customer => customer == null);

        UpdateQueuePositions();
    }

    private void UpdateQueuePositions()
    {
        if (frontPosition == null)
            return;

        Vector3 direction = lineDirection.normalized;

        for (int i = 0; i < customers.Count; i++)
        {
            Vector3 targetPosition =
                frontPosition.position - direction * spacing * i;

            customers[i].MoveToQueuePosition(targetPosition);
        }
    }
}
