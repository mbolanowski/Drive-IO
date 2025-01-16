using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ColorChange : MonoBehaviour
{
    public Light spotlight; // Use UnityEngine's Light component
    public TextMeshPro textMeshPro;

    public float maxDistance = 50f; // Max distance for color interpolation
    public float fadeDuration = 1.0f; // Duration for fading out
    private List<Transform> detectedObjects = new List<Transform>(); // List of detected objects
    private Coroutine fadeCoroutine; // To keep track of the current fade-out coroutine

    // Called when another collider enters the trigger collider attached to this GameObject
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AutonomousVehicle"))
        {
            detectedObjects.Add(other.transform); // Add the object to the list
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine); // Stop fading out if an object enters
                fadeCoroutine = null;
            }
            UpdateColor();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("AutonomousVehicle"))
        {
            UpdateColor();
        }
    }

    // Called when another collider exits the trigger collider attached to this GameObject
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("AutonomousVehicle"))
        {
            detectedObjects.Remove(other.transform); // Remove the object from the list
            UpdateColor();
        }
    }

    // Update the color based on the presence of "AutonomousVehicle"
    private void UpdateColor()
    {
        if (detectedObjects.Count > 0 && spotlight != null)
        {
            // Enable the spotlight
            spotlight.enabled = true;
            spotlight.intensity = 58.42f;

            // Find the closest object
            Transform closestObject = GetClosestObject();

            if (closestObject != null)
            {
                // Calculate the distance to the closest object
                float distance = Vector3.Distance(spotlight.transform.position, closestObject.position);

                // Normalize the distance based on the max distance
                float t = Mathf.Clamp01(distance / maxDistance);

                Color color = Color.Lerp(Color.red, Color.yellow, t);
                // Interpolate color from red (near) to yellow (far)
                spotlight.color = color;

                // Update the TextMeshPro text with the distance
                if (textMeshPro != null)
                {
                    textMeshPro.text = $"{distance:F1}m";
                    textMeshPro.color = color; // Reset opacity
                    textMeshPro.enabled = true; // Show the text
                }
            }
        }
        else
        {
            // Start fading out the spotlight and text
            if (fadeCoroutine == null)
            {
                fadeCoroutine = StartCoroutine(FadeOut());
            }
        }
    }

    // Coroutine to fade out the spotlight and text
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float initialIntensity = spotlight.intensity;
        Color initialTextColor = textMeshPro != null ? textMeshPro.color : Color.clear;

        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;

            // Fade the spotlight's intensity
            if (spotlight != null)
            {
                spotlight.intensity = Mathf.Lerp(initialIntensity, 0, t);
            }

            // Fade the text opacity
            if (textMeshPro != null)
            {
                textMeshPro.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, Mathf.Lerp(initialTextColor.a, 0, t));
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set
        if (spotlight != null)
        {
            spotlight.intensity = 0;
            spotlight.enabled = false;
        }

        if (textMeshPro != null)
        {
            textMeshPro.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, 0);
            textMeshPro.enabled = false;
        }

        fadeCoroutine = null; // Clear the coroutine reference
    }

    // Find the closest object from the list of detected objects
    private Transform GetClosestObject()
    {
        Transform closestObject = null;
        float closestDistance = float.MaxValue;

        foreach (Transform obj in detectedObjects)
        {
            if (obj != null) // Ensure the object still exists
            {
                float distance = Vector3.Distance(spotlight.transform.position, obj.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                }
            }
        }

        return closestObject;
    }
}
