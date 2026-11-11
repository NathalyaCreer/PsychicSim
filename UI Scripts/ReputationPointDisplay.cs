using TMPro;
using UnityEngine;

public class ReputationPointsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text reputationPointsText;

    private void OnEnable()
    {
        if (ReputationSystem.Instance == null)
            return;

        ReputationSystem.Instance.OnReputationPointsChanged += UpdateDisplay;
        UpdateDisplay(ReputationSystem.Instance.ReputationPoints);
    }

    private void OnDisable()
    {
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.OnReputationPointsChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int points)
    {
        if (reputationPointsText != null)
            reputationPointsText.text = $"Reputation Points: {points}";
    }
}