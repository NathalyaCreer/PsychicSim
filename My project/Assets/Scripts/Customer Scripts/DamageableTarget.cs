using UnityEngine;
using System;

public class DamageableTarget : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDeath;
    public event Action<float> OnDamaged;

    private PossessedCustomer possessedCustomer;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        // Only retrieve the possession controller.
        // Do not configure colliders, ragdoll bodies, Animator,
        // or NavMeshAgent here.
        possessedCustomer = GetComponent<PossessedCustomer>();

        if (possessedCustomer == null)
        {
            possessedCustomer = GetComponentInParent<PossessedCustomer>();
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        OnDamaged?.Invoke(damage);

        Debug.Log(
            $"{gameObject.name} took {damage} damage. " +
            $"Health: {CurrentHealth}/{maxHealth}"
        );

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log($"{gameObject.name} has died.");

        // Stop normal customer behavior and navigation.
        Customer customer = GetComponentInParent<Customer>();

        if (customer != null)
        {
            customer.PrepareForDeath();
        }

        // Notify other systems that this target has died.
        OnDeath?.Invoke();

        // Let PossessedCustomer manage the physical ragdoll.
        if (possessedCustomer != null)
        {
            possessedCustomer.ActivateDeathRagdoll();
        }
    }


    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }
}
