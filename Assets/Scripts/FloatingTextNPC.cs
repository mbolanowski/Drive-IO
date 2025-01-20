using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static System.Net.WebRequestMethods;

public class FloatingTextNPC : MonoBehaviour
{
    public string displayText = ""; // Text to display
    public Vector3 offset = new Vector3(0, 5, 1); // Offset from the GameObject
    public float textSize = 3.5f; // Size of the text
    public Color textColor = Color.white; // Color of the text

    public TextMeshPro textMeshPro;

    private GameObject textObject;

    void Start()
    {
        // Create a new GameObject for the text
        textObject = new GameObject("FloatingText");

        // Attach the TextMeshPro component
        textMeshPro = textObject.AddComponent<TextMeshPro>();

        AssignFontAndMaterial();

        textMeshPro.text = displayText;
        textMeshPro.fontSize = textSize;
        textMeshPro.color = textColor;
    }
    void Update()
    {
        //
        //Debug.Log(displayText);
        if (displayText == "input nickname here...")
        {
            textMeshPro.text = "default";
            displayText = "default";
        }

        // Make the text face the camera
        textMeshPro.alignment = TextAlignmentOptions.Center;

        // Parent the text to this GameObject
        textObject.transform.SetParent(transform);

        // Position the text above the GameObject with an offset
        textObject.transform.localPosition = offset;

        // Adjust the rotation so the text faces the camera
        textObject.transform.rotation = Quaternion.identity;

        // Optional: Keep the text facing the camera
        if (Camera.main != null)
        {
            // Get the direction from the text to the camera
            Vector3 directionToCamera = textMeshPro.transform.position - Camera.main.transform.position;

            // Zero out the Y component to avoid rotating around the Y axis
            directionToCamera.x = 0;
            directionToCamera.z = 0;

            // Adjust the rotation so the text faces the camera, but without rotating around the Y-axis
            textMeshPro.transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    void AssignFontAndMaterial()
    {
        // Load the LiberationSans SDF font
        TMP_FontAsset liberationSansFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Load the Drop Shadow material
        Material dropShadowMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow");

        if (liberationSansFont != null)
        {
            textMeshPro.font = liberationSansFont;

            if (dropShadowMaterial != null)
            {
                textMeshPro.fontMaterial = dropShadowMaterial; // Apply the Drop Shadow material
            }
            else
            {
                Debug.LogError("Drop Shadow material not found! Make sure it is located in a Resources folder.");
            }
        }
        else
        {
            Debug.LogError("LiberationSans SDF font not found! Make sure it is located in a Resources folder.");
        }
    }
}
