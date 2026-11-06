using UnityEngine;

public class StoreSignInteraction : MonoBehaviour
{
    [SerializeField] private WorkShift workShift;

    [Header("Sign Visuals")]
    [SerializeField] private GameObject closedSignVisual;
    [SerializeField] private GameObject openSignVisual;

    private void OnEnable()
    {
        if (workShift != null)
        {
            workShift.PhaseChanged += HandlePhaseChanged;
            UpdateSign(workShift.CurrentPhase);
        }
    }

    private void OnDisable()
    {
        if (workShift != null)
            workShift.PhaseChanged -= HandlePhaseChanged;
    }

    public void Interact()
    {
        if (workShift == null)
        {
            Debug.LogError("StoreSignInteraction: WorkShift is not assigned.", this);
            return;
        }

        // The player can only open the store during Dawn.
        if (workShift.CurrentPhase == WorkShift.ShiftPhase.Dawn)
        {
            workShift.OpenStore();
        }
        else
        {
            Debug.Log("The store can only be opened at Dawn.", this);
        }
    }

    private void HandlePhaseChanged(WorkShift.ShiftPhase newPhase)
    {
        UpdateSign(newPhase);
    }

    private void UpdateSign(WorkShift.ShiftPhase phase)
    {
        bool storeIsOpen = phase == WorkShift.ShiftPhase.Daytime;

        if (closedSignVisual != null)
            closedSignVisual.SetActive(!storeIsOpen);

        if (openSignVisual != null)
            openSignVisual.SetActive(storeIsOpen);
    }
}