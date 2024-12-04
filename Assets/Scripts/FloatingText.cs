using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public string displayText = "Hello, World!"; // Text to display
    public Vector3 offset = new Vector3(0, 2, 0); // Offset from the GameObject
    public float textSize = 1.0f; // Size of the text
    public Color textColor = Color.white; // Color of the text

    private TextMeshPro textMeshPro;

    void Start()
    {
        // Create a new GameObject for the text
        GameObject textObject = new GameObject("FloatingText");

        // Attach the TextMeshPro component
        textMeshPro = textObject.AddComponent<TextMeshPro>();

        // Set the text properties
        textMeshPro.text = displayText;
        textMeshPro.fontSize = textSize;
        textMeshPro.color = textColor;

        // Make the text face the camera
        textMeshPro.alignment = TextAlignmentOptions.Center;

        // Parent the text to this GameObject
        textObject.transform.SetParent(transform);

        // Position the text above the GameObject with an offset
        textObject.transform.localPosition = offset;

        // Adjust the rotation so the text faces the camera
        textObject.transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        // Optional: Keep the text facing the camera
        if (Camera.main != null)
        {
            textMeshPro.transform.rotation = Quaternion.LookRotation(
                textMeshPro.transform.position - Camera.main.transform.position
            );
        }
    }
}
