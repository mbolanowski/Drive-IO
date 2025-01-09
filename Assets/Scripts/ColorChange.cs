using UnityEngine;

public class ColorChange : MonoBehaviour
{
    // The GameObject to check and change color
    public GameObject colorChangeObject;

    // The color to apply when an "AutonomousVehicle" is found
    public Color detectedColor = Color.red;

    // The color to apply when no "AutonomousVehicle" is found
    public Color defaultColor = Color.white;

    public int autonomousVehicleCount = 0;

    // Called when another collider enters the trigger collider attached to this GameObject
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AutonomousVehicle"))
        {
            autonomousVehicleCount++;
            UpdateColor();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        UpdateColor();
    }

    // Called when another collider exits the trigger collider attached to this GameObject
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("AutonomousVehicle"))
        {
            autonomousVehicleCount--;
            UpdateColor();
        }
    }

    // Update the color based on the presence of "AutonomousVehicle"
    private void UpdateColor()
    {
        if (colorChangeObject != null)
        {
            Renderer renderer = colorChangeObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = autonomousVehicleCount > 0 ? detectedColor : defaultColor;
            }
            else
            {
                Debug.LogWarning("The colorChangeObject does not have a Renderer component.");
            }
        }
        else
        {
            Debug.LogWarning("colorChangeObject is not assigned.");
        }
    }
}