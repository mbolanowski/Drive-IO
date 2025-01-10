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

            // Activate the text and reset vanish timer
            infoText.gameObject.SetActive(true);

            // Cancel any previous vanish timer and start a new one
            CancelInvoke("FadeOutText");
            Invoke("FadeOutText", 3f); // Fade out after 3 seconds
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

            // Animate the text (slide in from the left)
            SlideInText(penaltyText);

            // Activate the text and reset vanish timer
            penaltyText.gameObject.SetActive(true);

            // Cancel any previous vanish timer and start a new one
            CancelInvoke("FadeOutPenaltyText");
            Invoke("FadeOutPenaltyText", 3f); // Fade out after 3 seconds
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
        LeanTween.scale(textMeshPro.gameObject, originalScale * 1.1f, 0.15f).setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(textMeshPro.gameObject, originalScale, 0.1f).setEase(LeanTweenType.easeInOutQuad);
            });
    }

    // Function to animate the "Penalty" TextMeshPro by sliding in from the left
    private void SlideInText(TextMeshPro textMeshPro)
    {
        // Get the RectTransform component
        RectTransform rectTransform = textMeshPro.GetComponent<RectTransform>();

        // Start position is off-screen to the left (you can adjust this value based on your screen width)
        Vector3 offScreenPosition = new Vector3(rectTransform.position.x - 10f, rectTransform.localPosition.y, rectTransform.localPosition.z);

        // Set the text to start off-screen to the left and animate it to its original position
        rectTransform.localPosition = offScreenPosition;

        // Animate the text to slide in from the left to its original position
        LeanTween.moveLocalX(rectTransform.gameObject, 0f, 0.9f).setEase(LeanTweenType.easeOutQuad);
    }

    // Function to fade out the "Info" TextMeshPro after 3 seconds by changing vertex color
    private void FadeOutText()
    {
        // Get the mesh of the text
        Mesh mesh = infoText.mesh;

        // Get the current vertex colors
        Color[] colors = mesh.colors;

        // Animate the alpha value of the vertex colors to 0 (fade out)
        LeanTween.value(infoText.gameObject, 1f, 0f, 1f)
            .setOnUpdate((float val) =>
            {
                // Fade out the vertex color by adjusting alpha
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i].a = val; // Modify the alpha value of each vertex color
                }

                // Apply the modified colors back to the mesh
                mesh.colors = colors;
            })
            .setOnComplete(() =>
            {
                infoText.gameObject.SetActive(false); // Deactivate after fade-out
            });
    }

    // Function to fade out the "Penalty" TextMeshPro after 3 seconds by changing vertex color
    private void FadeOutPenaltyText()
    {
        // Get the mesh of the text
        Mesh mesh = penaltyText.mesh;

        // Get the current vertex colors
        Color[] colors = mesh.colors;

        // Start fading out (move off-screen and fade out simultaneously)
        LeanTween.moveLocalX(penaltyText.gameObject, penaltyText.transform.localPosition.x - 10f, 0.5f).setEase(LeanTweenType.easeInOutQuad);

        // Animate the alpha value of the vertex colors to 0 (fade out)
        LeanTween.value(penaltyText.gameObject, 1f, 0f, 1f)
            .setOnUpdate((float val) =>
            {
                // Fade out the vertex color by adjusting alpha
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i].a = val; // Modify the alpha value of each vertex color
                }

                // Apply the modified colors back to the mesh
                mesh.colors = colors;
            })
            .setOnComplete(() =>
            {
                penaltyText.gameObject.SetActive(false); // Deactivate after fade-out
            });
    }
}
