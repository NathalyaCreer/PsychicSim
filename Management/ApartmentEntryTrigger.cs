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

    private void UpdatePrompt()
    {
        if (confirmationPanel == null || workShift == null)
            return;

        // Only offer apartment entry after the store closes.
        bool canGoUpstairs =
            workShift.CurrentPhase == WorkShift.ShiftPhase.Dusk;

        confirmationPanel.SetActive(playerInside && canGoUpstairs);
    }

    // Hook this to the confirmation button's OnClick.
    public void ConfirmGoUpstairs()
    {
        if (!playerInside || workShift == null)
            return;

        workShift.GoUpstairs();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    // Hook this to the cancel button's OnClick.
    public void CancelGoUpstairs()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}