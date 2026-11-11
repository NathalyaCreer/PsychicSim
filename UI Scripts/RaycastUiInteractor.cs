using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastUiInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask uiLayer; // Set this to your UI layer (usually Layer 5)

    private Camera cam;

    void Start()
    {
        // Get the camera reference
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        // Check for the "E" key press
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithUI();
        }
    }

    void TryInteractWithUI()
    {
        // Create a ray from the center of the viewport (crosshair position)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Structure to hold EventSystem raycast data
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = new Vector2(Screen.width / 2, Screen.height / 2);

        // List to store all UI objects hit by the raycast
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Loop through everything the UI raycast hit
        foreach (RaycastResult result in results)
        {
            // Check if the hit object is within your maximum interaction distance
            if (result.distance <= interactDistance)
            {
                // Check if the object (or its parents) has a UI Button component
                Button button = result.gameObject.GetComponentInParent<Button>();
                
                if (button != null && button.interactable)
                {
                    // Trigger the button's onClick event programmatically
                    button.onClick.Invoke();
                    break; // Stop looking after hitting the first valid button
                }
            }
        }
    }
}
