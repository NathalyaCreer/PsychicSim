using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Starting Money")]
    [SerializeField, Min(0)] private int startingMoney = 0;

    public int Balance { get; private set; }

    public event Action<int> OnBalanceChanged;

    private const string MoneySaveKey = "PlayerMoney";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Load saved balance, or use the starting amount for a new save.
        Balance = PlayerPrefs.GetInt(MoneySaveKey, startingMoney);
        OnBalanceChanged?.Invoke(Balance);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("MoneyManager: AddMoney amount must be positive.", this);
            return;
        }

        Balance += amount;
        SaveBalance();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("MoneyManager: Spend amount must be positive.", this);
            return false;
        }

        if (Balance < amount)
        {
            Debug.Log("Not enough money. Current balance: " + Balance, this);
            return false;
        }

        Balance -= amount;
        SaveBalance();
        return true;
    }

    public void SetMoney(int amount)
    {
        Balance = Mathf.Max(0, amount);
        SaveBalance();
    }

    private void SaveBalance()
    {
        PlayerPrefs.SetInt(MoneySaveKey, Balance);
        PlayerPrefs.Save();

        OnBalanceChanged?.Invoke(Balance);
        Debug.Log("$" + Balance, this);
    }
}