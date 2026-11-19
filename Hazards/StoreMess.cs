using UnityEngine;

public class StoreMess : MonoBehaviour
{
    public enum MessType
    {
        Trash,
        Spill
    }

    [Header("Mess Information")]
    [SerializeField] private MessType messType;

    [Header("Cleanup")]
    [SerializeField] private float cleanupDuration = 1f;

    private bool isCleaned;
    private float cleanupProgress;

    public MessType Type => messType;
    public bool IsCleaned => isCleaned;
    public float CleanupProgress => cleanupProgress;
    public float CleanupDuration => cleanupDuration;

    public bool RequiresMop => messType == MessType.Spill;
    public bool RequiresTrashBag => messType == MessType.Trash;

    
    public void SetCleanupProgress(float progress)
    {
        if (isCleaned)
            return;

        cleanupProgress = Mathf.Clamp01(progress);
    }

    public void CompleteCleanup()
    {
        if (isCleaned)
            return;

        isCleaned = true;

        Debug.Log(
            $"{messType} cleaned up.",
            this
        );

        // Reputation integration will be added after cleanup works.
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (MessSpawner.Instance != null)
            MessSpawner.Instance.UnregisterMess(this);
    }

    private EmployeeAI claimedBy;

    public bool IsClaimed => claimedBy != null;

    public bool TryClaim(EmployeeAI employee)
    {
        if (employee == null)
            return false;

        if (claimedBy != null && claimedBy != employee)
            return false;

        claimedBy = employee;
        return true;
    }

    public void ReleaseClaim(EmployeeAI employee)
    {
        if (claimedBy == employee)
            claimedBy = null;
    }
}
