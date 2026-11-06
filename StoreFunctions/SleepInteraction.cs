
using UnityEngine;

public class SleepInteraction : MonoBehaviour
{
    [SerializeField] private WorkShift workShift;

    // Connect your bed's click interaction to this method.
    public void Interact()
    {
        if (workShift == null)
        {
            Debug.LogError("SleepInteraction: WorkShift is not assigned.", this);
            return;
        }

        if (workShift.CurrentPhase != WorkShift.ShiftPhase.Afterhours)
        {
            Debug.Log("The player can only sleep during Afterhours.", this);
            return;
        }

        workShift.SleepAndStartNextDay();

        Debug.Log("Player slept. Starting the next day.");
    }
}
