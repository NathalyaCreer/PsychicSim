using UnityEngine;

public class BillboardingUI : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private void Reset()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Make the UI face the same direction as the camera.
        transform.rotation = mainCamera.transform.rotation;
    }
}
