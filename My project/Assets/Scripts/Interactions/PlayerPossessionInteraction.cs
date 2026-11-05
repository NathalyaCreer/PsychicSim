
using UnityEngine;

public class PlayerPossessionInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform carryPoint;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask customerLayers = ~0;

    [Header("Push")]
    [SerializeField] private float pushStrength = 5f;
    [SerializeField] private float pushUpwardForce = 1.5f;

    [Header("Throw")]
    [SerializeField] private float throwStrength = 12f;
    [SerializeField] private float throwUpwardForce = 3.5f;

    private PossessedCustomer carriedCustomer;

    // Other interaction scripts can check this before using E.
    public bool IsCarryingCustomer => carriedCustomer != null;
    public bool IsTargetingPossessedCustomer { get; private set; }

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        IsTargetingPossessedCustomer = FindTarget() != null;

        if (carriedCustomer != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
                ThrowCarriedCustomer();

            return;
        }

        if (Input.GetKeyDown(KeyCode.E) &&
            IsTargetingPossessedCustomer)
        {
            InteractWithCustomer();
        }
    }

    private PossessedCustomer FindTarget()
    {
        if (playerCamera == null)
            return null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionRange,
                customerLayers,
                QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        PossessedCustomer target =
            hit.collider.GetComponentInParent<PossessedCustomer>();

        if (target == null)
            return null;

        if (!target.IsPossessed && !target.IsDeadBody)
            return null;

        return target;
    }

    private void InteractWithCustomer()
    {
        PossessedCustomer target = FindTarget();

        if (target == null)
            return;

        if (target.CurrentState ==
            PossessedCustomer.PossessionState.Possessed)
        {
            Vector3 pushDirection =
                playerCamera.transform.forward * pushStrength +
                Vector3.up * pushUpwardForce;

            target.ApplyPush(pushDirection);
            return;
        }

        bool canCarry =
            target.CurrentState ==
                PossessedCustomer.PossessionState.KnockedDown ||
            target.CurrentState ==
                PossessedCustomer.PossessionState.Dead;

        if (!canCarry)
            return;

        if (carryPoint == null)
        {
            Debug.LogWarning("Assign a Carry Point.", this);
            return;
        }

        if (target.TryBeginCarry() &&
            target.AttachToCarry(carryPoint))
        {
            carriedCustomer = target;
        }
    }


    private void ThrowCarriedCustomer()
    {
        if (carriedCustomer == null)
            return;

        Vector3 throwVelocity =
            playerCamera.transform.forward * throwStrength +
            Vector3.up * throwUpwardForce;

        carriedCustomer.ThrowFromCarry(throwVelocity);
        carriedCustomer = null;
    }

}
