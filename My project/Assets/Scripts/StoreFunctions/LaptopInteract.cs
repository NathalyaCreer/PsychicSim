using UnityEngine;

public class LaptopInteract : MonoBehaviour
{
    [Header("Laptop UI")]
    [SerializeField] private GameObject laptopUI;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private MonoBehaviour playerLook;

    private bool laptopIsOpen;

    private void Start()
    {
        if (laptopUI != null)
            laptopUI.SetActive(false);
    }

    public void Interact()
    {
        if (laptopIsOpen)
            return;

        OpenLaptop();
    }

    public void OpenLaptop()
    {
        if (laptopUI == null)
        {
            Debug.LogError("LaptopInteract: Laptop UI is not assigned.", this);
            return;
        }

        laptopIsOpen = true;

        laptopUI.SetActive(true);

        // Disable player movement and camera look while using the laptop.
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        // Show and unlock the mouse so the player can click UI.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseLaptop()
    {
        laptopIsOpen = false;

        if (laptopUI != null)
            laptopUI.SetActive(false);

        // Restore player controls.
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerLook != null)
            playerLook.enabled = true;

        // Hide and lock the mouse for gameplay.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
