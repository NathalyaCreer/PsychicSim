using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Customer))]
public class PossessedCustomer : MonoBehaviour
{
    public enum PossessionBehavior
    {
        Kill,
        Destroy
    }

    public enum PossessionState
    {
        Normal,
        Possessed,
        KnockedDown,
        Carried,
        Dead,
        DeadCarried,
        Removed
    }

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    [Header("Knockdown Recovery")]
    [SerializeField] private float fallCoolDown = 3f;

    [Header("Ragdoll")]
    [Tooltip("Assign the Animator controlling the customer's character.")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("Assign the NavMeshAgent on the customer root.")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Tooltip("Assign the customer's root collider, if it has one.")]
    [SerializeField] private Collider rootCollider;


    [Header("Destroy Behavior")]
    [SerializeField] private PossessedCustomerDestroy destroyBehavior;

    public PossessionBehavior CurrentBehavior { get; private set; }

    public PossessionState CurrentState { get; private set; } = PossessionState.Normal;

    public bool IsPossessed =>
        CurrentState == PossessionState.Possessed ||
        CurrentState == PossessionState.KnockedDown ||
        CurrentState == PossessionState.Carried;

    public bool IsDeadBody =>
        CurrentState == PossessionState.Dead ||
        CurrentState == PossessionState.DeadCarried;

    private Customer customer;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private bool ragdollActive;
    private Coroutine recoveryCoroutine;
    private bool isInsideStore;

    public bool IsInsideStore => isInsideStore;

    public void SetInsideStore(bool inside)
    {
        isInsideStore = inside;
    }

    private void Start()
    {
        if (navMeshAgent == null)
        {
            Debug.LogError(
                $"{name}: PossessedCustomer cannot find NavMeshAgent.",
                this
            );
            return;
        }

        Debug.Log(
            $"{name}: Agent enabled={navMeshAgent.enabled}, " +
            $"onNavMesh={navMeshAgent.isOnNavMesh}, " +
            $"isStopped={navMeshAgent.isStopped}, " +
            $"hasPath={navMeshAgent.hasPath}",
            this
        );
    }
    
    private void Awake()
    {
        customer = GetComponent<Customer>();

        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        if (destroyBehavior == null)
            destroyBehavior = GetComponent<PossessedCustomerDestroy>();

        CurrentHealth = maxHealth;

        // Ragdoll bones should have their own Rigidbody components.
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetRagdollColliders();

        SetRagdollEnabled(false);
    }

    private Collider[] GetRagdollColliders()
    {
        System.Collections.Generic.List<Collider> colliders =
            new System.Collections.Generic.List<Collider>();

        foreach (Rigidbody body in ragdollBodies)
        {
            Collider[] attached = body.GetComponents<Collider>();

            foreach (Collider col in attached)
            {
                if (col != null && col != rootCollider)
                    colliders.Add(col);
            }
        }

        return colliders.ToArray();
    }

    public bool BeginPossession(PossessionBehavior behavior)
    {
        if (!isInsideStore ||
            CurrentState != PossessionState.Normal ||
            customer == null)
        {
            return false;
        }

        CurrentBehavior = behavior;
        CurrentState = PossessionState.Possessed;

        customer.PrepareForPossession();

        // Start the fire sabotage only if Destroy possession succeeds.
        if (behavior == PossessionBehavior.Destroy)
        {
            if (destroyBehavior != null)
            {
                destroyBehavior.BeginDestroyMode();
            }
            else
            {
                Debug.LogWarning(
                    $"{name} has Destroy behavior but no PossessedCustomerDestroy component.",
                    this);
            }
        }

        Debug.Log($"{name} is possessed. Behavior: {behavior}", this);
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (!IsPossessed || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        Debug.Log(
            $"{name} took {damage} damage. Health: {CurrentHealth}/{maxHealth}",
            this
        );

        if (CurrentHealth <= 0f)
            KnockDown();
    }

    public void KnockDown()
    {
        // Only a standing possessed customer can be knocked down.
        if (CurrentState != PossessionState.Possessed)
            return;

        if (recoveryCoroutine != null)
            StopCoroutine(recoveryCoroutine);

        CurrentState = PossessionState.KnockedDown;

        // Enable the ragdoll so the customer falls naturally.
        SetRagdollEnabled(true);

        recoveryCoroutine = StartCoroutine(RecoverAfterFall());
    }

    private IEnumerator RecoverAfterFall()
    {
        yield return new WaitForSeconds(fallCoolDown);

        // Carrying, death, or removal must override recovery.
        if (CurrentState != PossessionState.KnockedDown)
        {
            recoveryCoroutine = null;
            yield break;
        }

        // Restore the normal possessed-customer state.
        SetRagdollEnabled(false);
        CurrentState = PossessionState.Possessed;

        recoveryCoroutine = null;

        Debug.Log($"{name} got back up after being pushed.", this);
    }

    public void ThrowOutOfStore()
    {
        if (!IsPossessed)
            return;

        if (destroyBehavior != null)
            destroyBehavior.InterruptSabotage();

        CurrentState = PossessionState.Removed;

        if (customer != null)
            customer.RemoveFromStoreAfterPossession();
        else
            Destroy(gameObject);
    }

    private void SetRagdollEnabled(bool enabled)
    {
        ragdollActive = enabled;

        if (characterAnimator != null)
            characterAnimator.enabled = !enabled;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = enabled;

                if (enabled)
                    navMeshAgent.ResetPath();
            }

            navMeshAgent.enabled = !enabled;
        }

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
                continue;

            body.isKinematic = !enabled;
            body.useGravity = enabled;

            if (enabled)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }

        // The root collider is disabled during ragdoll to avoid
        // fighting with the bone colliders.
        if (rootCollider != null)
            rootCollider.enabled = !enabled;
    }

    public bool IsRagdollActive => ragdollActive;


    public bool WasThrown { get; private set; }

    public void ApplyPush(Vector3 force)
    {
        if (CurrentState != PossessionState.Possessed)
            return;

        // Interrupt any fire preparation before knocking the customer down.
        if (destroyBehavior != null)
            destroyBehavior.InterruptSabotage();

        KnockDown();

        Rigidbody body = FindMainRagdollBody();

        if (body != null)
            body.AddForce(force, ForceMode.Impulse);
    }

    public bool TryBeginCarry()
    {
        if (CurrentState != PossessionState.KnockedDown &&
            CurrentState != PossessionState.Dead)
        {
            return false;
        }

        CurrentState = CurrentState == PossessionState.Dead
            ? PossessionState.DeadCarried
            : PossessionState.Carried;

        SetRagdollEnabled(true);

        return true;
    }

    public bool AttachToCarry(Transform carryPoint)
    {
        if ((CurrentState != PossessionState.Carried &&
            CurrentState != PossessionState.DeadCarried) ||
            carryPoint == null)
        {
            return false;
        }

        Rigidbody mainBody = FindMainRagdollBody();

        if (mainBody == null)
        {
            Debug.LogWarning($"{name}: No ragdoll Rigidbody found.", this);
            return false;
        }

        Vector3 rootToBodyOffset = transform.position - mainBody.position;

        transform.position = carryPoint.position + rootToBodyOffset;
        transform.SetParent(carryPoint, true);

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null) continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        if (rootCollider != null)
            rootCollider.enabled = false;

        return true;
    }

    public void ThrowFromCarry(Vector3 throwVelocity)
    {
        bool throwingDeadBody = CurrentState == PossessionState.DeadCarried;

        if (!throwingDeadBody && CurrentState != PossessionState.Carried)
            return;

        transform.SetParent(null, true);

        // A dead customer stays dead after being thrown.
        CurrentState = throwingDeadBody
            ? PossessionState.Dead
            : PossessionState.KnockedDown;

        WasThrown = true;

        foreach (Collider col in ragdollColliders)
        {
            if (col != null)
                col.enabled = true;
        }

        if (rootCollider != null)
            rootCollider.enabled = false;

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null) continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = false;
            body.useGravity = true;
        }

        Rigidbody mainBody = FindMainRagdollBody();

        if (mainBody != null)
            mainBody.AddForce(throwVelocity, ForceMode.VelocityChange);
    }



    private Rigidbody FindMainRagdollBody()
    {
        // Prefer the hips or pelvis for more natural whole-body movement.
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null) continue;

            string boneName = body.name.ToLower();

            if (boneName.Contains("hip") || boneName.Contains("pelvis"))
                return body;
        }

        return ragdollBodies.Length > 0 ? ragdollBodies[0] : null;
    }


    public void ActivateDeathRagdoll()
    {
        if (IsDeadBody || CurrentState == PossessionState.Removed)
            return;

        CurrentState = PossessionState.Dead;

        if (customer != null)
            customer.PrepareForDeath();

        SetRagdollEnabled(true);
    }

    public void MarkDeadBodyDisposed()
    {
        if (!IsDeadBody)
            return;

        CurrentState = PossessionState.Removed;

        if (customer != null)
            customer.RemoveFromStoreAfterPossession();
        else
            Destroy(gameObject);
    }


}

