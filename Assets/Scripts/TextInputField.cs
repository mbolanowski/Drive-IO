using UnityEngine;
using TMPro; // Use this if you're using TextMeshPro

public class TextInputField : MonoBehaviour
{
    public TextMeshPro textMesh; // Reference to the TextMeshPro component
    private bool isFocused = false; // Tracks whether the field is focused
    private string currentText = ""; // Stores the current text

    private void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
    }

    private void OnMouseDown()
    {
        isFocused = true; // Focus the field when clicked
    }

    private void Update()
    {
        if (isFocused)
        {
            // Handle keyboard input
            foreach (char c in Input.inputString)
            {
                if (c == '\b') // Backspace
                {
                    if (currentText.Length > 0)
                    {
                        currentText = currentText.Substring(0, currentText.Length - 1);
                    }
                }
                else if (c == '\n' || c == '\r') // Enter
                {
                    isFocused = false; // Unfocus on Enter
                }
                else
                {
                    currentText += c;
                }
            }

            // Update the text display
            textMesh.text = currentText;
        }

        // Optional: Click outside to unfocus
        if (Input.GetMouseButtonDown(0) && !IsMouseOverText())
        {
            isFocused = false;
        }
    }

    private bool IsMouseOverText()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform;
    }
}
