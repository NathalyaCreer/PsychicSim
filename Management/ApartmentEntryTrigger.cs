
using UnityEngine;

public class ApartmentEntryTrigger : MonoBehaviour
{
    [SerializeField] private WorkShift workShift;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void Start()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (workShift == null)
            workShift = WorkShift.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = true;
        UpdatePrompt();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = true;
        UpdatePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private bool CanGoUpstairs()
    {
        WorkShift shift = WorkShift.Instance;

        if (shift == null)
            shift = workShift;

        CustomerSpawner spawner = CustomerSpawner.Instance;

        return shift != null &&
               shift.CurrentPhase == WorkShift.ShiftPhase.Dusk &&
               spawner != null &&
               !spawner.HasCustomersRemaining;
    }

    private void UpdatePrompt()
    {
        if (confirmationPanel == null)
            return;

        confirmationPanel.SetActive(
            playerInside && CanGoUpstairs()
        );
    }

    public void ConfirmGoUpstairs()
    {
        if (!playerInside)
            return;

        if (!CanGoUpstairs())
        {
            Debug.LogWarning(
                "You cannot go upstairs until all customers have left the store."
            );

            UpdatePrompt();
            return;
        }

        WorkShift.Instance.GoUpstairs();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    public void CancelGoUpstairs()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
