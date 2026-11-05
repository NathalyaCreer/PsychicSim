
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PossessedCustomer))]
public class PossessionKillAI : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 1.4f;
    [SerializeField] private float targetRefreshInterval = 0.25f;

    private PossessedCustomer possessedCustomer;
    private DamageableTarget selfHealth;
    private NavMeshAgent agent;
    private Customer customer;

    private DamageableTarget currentTarget;
    private float nextAttackTime;
    private float nextTargetRefreshTime;
    private bool wasActivelyHunting;

    private void Awake()
    {
        customer = GetComponent<Customer>();
        possessedCustomer = GetComponent<PossessedCustomer>();
        selfHealth = GetComponent<DamageableTarget>();
        agent = GetComponent<NavMeshAgent>();

        // Do not change the agent's stopping distance here.
        // Customer.cs uses this same agent for normal movement.
    }

    private void Update()
    {
        bool shouldHunt =
            possessedCustomer != null &&
            possessedCustomer.CurrentBehavior ==
                PossessedCustomer.PossessionBehavior.Kill &&
            possessedCustomer.CurrentState ==
                PossessedCustomer.PossessionState.Possessed &&
            (selfHealth == null || !selfHealth.IsDead) &&
            (customer == null ||
                customer.CurrentState != Customer.CustomerState.Leaving);

        // When inactive, do NOT stop or clear the NavMeshAgent.
        if (!shouldHunt)
        {
            currentTarget = null;
            wasActivelyHunting = false;
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // Only initialize pursuit when entering active Kill behavior.
        if (!wasActivelyHunting)
        {
            wasActivelyHunting = true;
            nextTargetRefreshTime = 0f;
        }

        if (currentTarget == null ||
            currentTarget.IsDead ||
            Time.time >= nextTargetRefreshTime)
        {
            FindNearestTarget();
            nextTargetRefreshTime =
                Time.time + targetRefreshInterval;
        }

        if (currentTarget == null)
        {
            // Stop only while the AI is actively hunting.
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        Vector3 targetPosition = currentTarget.transform.position;

        float distance = Vector3.Distance(
            transform.position,
            targetPosition
        );

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            FaceTarget();

            if (Time.time >= nextAttackTime)
                AttackTarget();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }
    }

    private void FindNearestTarget()
    {
        currentTarget = null;
        float closestDistance = detectionRange;

        DamageableTarget[] targets =
            FindObjectsByType<DamageableTarget>(FindObjectsSortMode.None);

        foreach (DamageableTarget target in targets)
        {
            if (target == null ||
                target == selfHealth ||
                target.IsDead ||
                !target.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Don't target another possessed customer.
            PossessedCustomer otherPossessed =
                target.GetComponent<PossessedCustomer>();

            if (otherPossessed != null &&
                otherPossessed.IsPossessed)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                target.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = target;
            }
        }
    }

    private void AttackTarget()
    {
        if (currentTarget == null || currentTarget.IsDead)
            return;

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.transform.position
        );

        if (distance > attackRange)
            return;

        nextAttackTime = Time.time + attackCooldown;

        // This matches the two-argument overload in your existing code.
        currentTarget.TakeDamage(attackDamage);

        Debug.Log(
            $"{name} attacked {currentTarget.name} for {attackDamage} damage.",
            this
        );
    }

    private void FaceTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 direction =
            currentTarget.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                360f * Time.deltaTime
            );
        }
    }
}
