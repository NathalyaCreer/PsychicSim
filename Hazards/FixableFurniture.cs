using UnityEngine;
using System;

public class FixableFurniture : MonoBehaviour
{
    [Header("Furniture Health")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int currentHealth = 100;

    [Header("Repair Settings")]
    [SerializeField, Min(1)] private int repairHitsRequired = 3;
    [SerializeField, Min(0)] private int repairCost = 25;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int RepairCost => repairCost;

    public bool IsBroken => currentHealth <= 0;
    public bool IsFixed => currentHealth >= maxHealth;
    public bool NeedsRepair => currentHealth < maxHealth;

    public int RepairHitsRemaining =>
        Mathf.Max(0, repairHitsRequired - repairHits);

    public event Action OnRepaired;

    private int repairHits;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsBroken)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"{name} took {damage} damage. Health: {currentHealth}/{maxHealth}",
            this
        );
    }

    // Use this when something completely destroys or burns furniture.
    public void SetBroken()
    {
        currentHealth = 0;
        repairHits = 0;

        Debug.Log($"{name} is broken and needs repair.", this);
    }

    // Called by the hammer each time it successfully hits furniture.
    public bool TryRepairHit()
    {

        Debug.Log(
            $"Hammer hit {name}. Health: {currentHealth}/{maxHealth}",
            this
        );

        if (!NeedsRepair)
        {
            Debug.Log($"{name} does not need repairs.", this);
            return false;
        }

        if (!NeedsRepair)
            return false;

        repairHits++;

        Debug.Log(
            $"{name}: Repair hit {repairHits}/{repairHitsRequired}.",
            this
        );

        if (repairHits < repairHitsRequired)
            return false;

        MoneyManager moneyManager = MoneyManager.Instance;

        if (moneyManager == null)
        {
            Debug.LogWarning(
                "Furniture repair failed: MoneyManager.Instance is missing.",
                this
            );

            repairHits = repairHitsRequired - 1;
            return false;
        }

        if (!moneyManager.TrySpendMoney(repairCost))
        {
            Debug.Log(
                $"Not enough money to repair {name}. Repair cost: ${repairCost}.",
                this
            );

            // Let the player try again after earning more money.
            repairHits = repairHitsRequired - 1;
            return false;
        }

        Debug.Log(
            MoneyManager.Instance == null
                ? "MoneyManager is still missing"
                : $"MoneyManager found. Current Balance: ${MoneyManager.Instance.Balance}"
        );

        RestoreFurniture();

        return true;
    }

    private void RestoreFurniture()
    {
        currentHealth = maxHealth;
        repairHits = 0;

        Debug.Log($"{name} has been fully repaired!", this);

        OnRepaired?.Invoke();
    }
}