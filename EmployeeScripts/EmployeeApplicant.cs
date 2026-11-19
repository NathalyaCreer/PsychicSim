
using UnityEngine;
using System;

public enum EmployeeRole
{
    Cashier,
    Maintenance
}

[Serializable]
public class EmployeeApplicant
{
    public string applicantId;
    public string employeeName;

    public EmployeeRole role;

    [Range(1, 100)] public int fortitude;
    [Range(1, 100)] public int workEthic;
    [Range(1, 100)] public int speed;

    public int dailyPay;

    [Header("Health")]
    [Min(1)] public int maxHealth = 100;
    [Min(0)] public int currentHealth = 100;

    [Header("Employment Status")]
    public bool willQuit;
    public bool isDead;

    public bool IsAlive => !isDead && currentHealth > 0;

    public EmployeeApplicant(
        string id,
        string name,
        EmployeeRole role,
        int fortitude,
        int workEthic,
        int speed,
        int dailyPay)
    {
        applicantId = id;
        employeeName = name;
        this.role = role;
        this.fortitude = fortitude;
        this.workEthic = workEthic;
        this.speed = speed;
        this.dailyPay = dailyPay;

        maxHealth = 100;
        currentHealth = maxHealth;

        willQuit = false;
        isDead = false;
    }

    public bool TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"{employeeName} took {damage} damage. " +
            $"Health: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            isDead = true;

            Debug.LogWarning(
                $"{employeeName} has died."
            );

            return true;
        }

        return false;
    }

    public void MarkWillQuit()
    {
        if (isDead)
            return;

        willQuit = true;

        Debug.LogWarning(
            $"{employeeName} is leaving employment."
        );
    }
}
