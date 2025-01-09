using TMPro;
using UnityEngine;

public class WarningSystemController : MonoBehaviour
{
    // Reference to the "Info" and "Penalty" TextMeshPro components
    private TextMeshPro infoText;
    private TextMeshPro penaltyText;

    void Start()
    {
        // Get references to the child TextMeshPro components
        infoText = transform.Find("Info").GetComponent<TextMeshPro>();
        penaltyText = transform.Find("Penalty").GetComponent<TextMeshPro>();

        if (infoText == null || penaltyText == null)
        {
            Debug.LogError("TextMeshPro components 'Info' or 'Penalty' not found.");
        }
    }

    // Function to change the text in the "Info" TextMeshPro with animation
    public void SetInfoText(string newText)
    {
        if (infoText != null)
        {
            // Set the new text
            infoText.text = newText;

            // Animate the text (scale up, scale down, shake, and restore rotation)
            AnimateText(infoText);
        }
        else
        {
            Debug.LogWarning("Info TextMeshPro component is not assigned.");
        }
    }

    // Function to change the text in the "Penalty" TextMeshPro with animation
    public void SetPenaltyText(string newText)
    {
        if (penaltyText != null)
        {
            // Set the new text
            penaltyText.text = newText;

            // Animate the text (scale up, scale down, shake, and restore rotation)
            AnimateText(penaltyText);
        }
        else
        {
            Debug.LogWarning("Penalty TextMeshPro component is not assigned.");
        }
    }

    // Function to animate TextMeshPro text (scale, shake, and rotate back)
    private void AnimateText(TextMeshPro textMeshPro)
    {
        // Store the original scale and rotation
        Vector3 originalScale = textMeshPro.transform.localScale;
        Quaternion originalRotation = textMeshPro.transform.rotation;

        // Scale up and down
        LeanTween.scale(textMeshPro.gameObject, originalScale * 1.2f, 0.1f).setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(textMeshPro.gameObject, originalScale, 0.1f).setEase(LeanTweenType.easeInOutQuad);
            });

        // Shake along the Y-axis
        LeanTween.moveLocalY(textMeshPro.gameObject, textMeshPro.transform.localPosition.y + 10f, 0.1f).setEase(LeanTweenType.easeInOutQuad)
            .setLoopPingPong(1); // Ping-pong effect (move back and forth)

        // Return the rotation to its starting point
        LeanTween.rotate(textMeshPro.gameObject, originalRotation.eulerAngles, 0.2f).setEase(LeanTweenType.easeInOutQuad);
    }
}
