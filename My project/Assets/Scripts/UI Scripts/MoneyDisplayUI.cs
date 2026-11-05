using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text balanceText;

    private bool subscribed;

    private void OnEnable()
    {
        StartCoroutine(WaitForMoneyManager());
    }

    private IEnumerator WaitForMoneyManager()
    {
        // Wait until MoneyManager has initialized its singleton.
        while (MoneyManager.Instance == null)
            yield return null;

        MoneyManager.Instance.OnBalanceChanged += UpdateBalance;
        subscribed = true;

        // Display the current balance immediately.
        UpdateBalance(MoneyManager.Instance.Balance);
    }

    private void OnDisable()
    {
        if (subscribed && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnBalanceChanged -= UpdateBalance;
            subscribed = false;
        }
    }

    private void UpdateBalance(int amount)
    {
        if (balanceText != null)
            balanceText.text = $"${amount}";
    }
}