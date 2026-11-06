using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PossessedCustomer))]
public class DeadBodyWitness : MonoBehaviour
{
    [Header("Witness Detection")]
    [SerializeField] private float witnessRange = 8f;
    [SerializeField, Range(0f, 360f)]
    private float fieldOfView = 120f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private readonly HashSet<Customer> witnesses =
        new HashSet<Customer>();

    private PossessedCustomer body;
    private float nextCheckTime;

    private void Awake()
    {
        body = GetComponent<PossessedCustomer>();
    }

    private void Update()
    {
        if (body == null || !body.IsDeadBody)
            return;

        if (body.CurrentState ==
            PossessedCustomer.PossessionState.DeadCarried)
            return;

        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;
        CheckForWitnesses();
    }

    private void CheckForWitnesses()
    {
        Collider[] nearby = Physics.OverlapSphere(
            transform.position,
            witnessRange
        );

        foreach (Collider col in nearby)
        {
            Customer customer =
                col.GetComponentInParent<Customer>();

            if (customer == null ||
                customer == GetComponent<Customer>() ||
                customer.IsDead ||
                witnesses.Contains(customer))
            {
                continue;
            }

            if (!CanSeeBody(customer))
                continue;

            witnesses.Add(customer);

            ReputationSystem reputation =
                ReputationSystem.Instance;

            if (reputation != null)
            {
                reputation.AddReputation(
                    -1,
                    $"Customer witnessed dead body: {customer.name}"
                );
            }

            Debug.Log(
                $"{customer.name} witnessed {name}. Reputation -1.",
                customer
            );
        }
    }

    private bool CanSeeBody(Customer customer)
    {
        Vector3 eyePosition =
            customer.transform.position +
            Vector3.up * eyeHeight;

        Vector3 targetPosition =
            transform.position + Vector3.up * 0.5f;

        Vector3 direction = targetPosition - eyePosition;
        float distance = direction.magnitude;

        if (distance > witnessRange || distance <= 0.01f)
            return false;

        if (Vector3.Angle(
                customer.transform.forward,
                direction) > fieldOfView * 0.5f)
        {
            return false;
        }

        Ray ray = new Ray(eyePosition, direction.normalized);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            // Ignore the witness's own colliders.
            if (hit.collider.GetComponentInParent<Customer>() == customer)
                continue;

            // The body is visible if the first relevant collider
            // belongs to the dead customer's ragdoll.
            if (hit.collider.GetComponentInParent<PossessedCustomer>() == body)
                return true;

            // Another object blocks the view.
            return false;
        }

        return false;
    }
}
