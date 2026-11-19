
using System.Collections.Generic;
using UnityEngine;

public class QueueLine : MonoBehaviour
{
    [Header("Queue Layout")]
    [SerializeField] private Transform frontPosition;
    [SerializeField] private Vector3 queueDirection = Vector3.back;
    [SerializeField] private float spacing = 1.25f;

    private readonly List<Customer> customers = new List<Customer>();

    [Header("Cashier")]
    private EmployeeAI assignedCashier;
    private Customer servingCustomer;

    public EmployeeAI AssignedCashier => assignedCashier;
    public bool HasCashier => assignedCashier != null;
    public Customer ServingCustomer => servingCustomer;
    public int CustomerCount => customers.Count;

    // --------------------------------------------------
    // CASHIER ASSIGNMENT
    // --------------------------------------------------

    public void AssignCashier(EmployeeAI cashier)
    {
        if (assignedCashier == cashier)
            return;

        if (servingCustomer != null)
        {
            Debug.LogWarning(
                $"{name}: Cannot change cashier while a customer is being served.",
                this
            );

            return;
        }

        assignedCashier = cashier;

        Debug.Log(
            assignedCashier != null
                ? $"{name}: {assignedCashier.EmployeeName} assigned as cashier."
                : $"{name}: Cashier assignment cleared.",
            this
        );
    }

    // --------------------------------------------------
    // CUSTOMER QUEUE
    // --------------------------------------------------

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

        if (servingCustomer == customer)
            servingCustomer = null;

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
            Debug.LogError(
                "QueueLine: Customer or front position is missing.",
                this
            );

            return false;
        }

        bool isFront = IsAtFront(customer);

        bool isNearFront =
            customer.IsNearQueuePosition(frontPosition.position);

        return isFront && isNearFront;
    }

    // --------------------------------------------------
    // CASHIER CUSTOMER CLAIMING
    // --------------------------------------------------

    public bool TryClaimNextCustomer(
        EmployeeAI cashier,
        out Customer customer
    )
    {
        customer = null;

        if (cashier == null)
        {
            Debug.LogWarning($"{name}: Cashier reference is null.");
            return false;
        }

        if (assignedCashier != cashier)
        {
            Debug.LogWarning(
                $"{name}: Assigned cashier does not match requesting employee. " +
                $"Assigned={assignedCashier}, Requesting={cashier.name}"
            );
            return false;
        }

        if (servingCustomer != null)
        {
            Debug.Log(
                $"{name}: Already serving {servingCustomer.name}."
            );
            return false;
        }

        if (customers.Count == 0)
        {
            Debug.Log($"{name}: No customers in queue.");
            return false;
        }

        Customer nextCustomer = customers[0];

        if (nextCustomer == null)
        {
            Debug.LogWarning($"{name}: First customer in queue is null.");
            return false;
        }

        if (nextCustomer.IsDead || nextCustomer.HasPaid)
        {
            Debug.LogWarning(
                $"{name}: First customer cannot be served. " +
                $"Dead={nextCustomer.IsDead}, Paid={nextCustomer.HasPaid}"
            );
            return false;
        }

        bool ready = IsReadyForCheckout(nextCustomer);

        if (!ready)
        {
            Debug.Log(
                $"{name}: {nextCustomer.name} isn't ready for checkout. " +
                $"AtFront={IsAtFront(nextCustomer)}"
            );
            return false;
        }

        servingCustomer = nextCustomer;
        customer = nextCustomer;

        Debug.Log(
            $"{cashier.EmployeeName} claimed {customer.name} for checkout.",
            this
        );

        return true;
    }

    public void ReleaseCustomer(
        EmployeeAI cashier,
        Customer customer
    )
    {
        if (customer == null)
            return;

        if (assignedCashier != cashier)
            return;

        if (servingCustomer != customer)
            return;

        servingCustomer = null;

        Debug.Log(
            $"{cashier.EmployeeName} released {customer.name} from checkout.",
            this
        );
    }

    // --------------------------------------------------
    // QUEUE UPDATES
    // --------------------------------------------------

    private void Update()
    {
        // Remove customers that have been destroyed.
        customers.RemoveAll(customer => customer == null);

        if (servingCustomer == null)
            servingCustomer = null;

        UpdateQueuePositions();
    }

    private void UpdateQueuePositions()
    {
        if (frontPosition == null)
            return;

        Vector3 direction = queueDirection.normalized;

        for (int i = 0; i < customers.Count; i++)
        {
            if (customers[i] == null)
                continue;

            Vector3 targetPosition =
                frontPosition.position - direction * spacing * i;

            customers[i].MoveToQueuePosition(targetPosition);
        }
    }
}
