using UnityEngine;

public class CameraUPDOWN : MonoBehaviour
{
    public Transform focusObject;              // The object the camera is focused on
    public float verticalMoveAmount = 11.2f;     // Vertical movement amount (up/down)
    public LayerMask playerLayer;              // Layer to detect the player

    [Header("Direction Setup")]
    public float directionThreshold = 0.7f;    // Threshold for vertical direction detection

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer.value) != 0)
        {
            Vector3 direction = GetVerticalDirection(other.transform.position);
            MoveFocusObject(direction);
        }
    }

    private Vector3 GetVerticalDirection(Vector3 playerPosition)
    {
        Vector3 relativePosition = playerPosition - transform.position;
        float forwardDot = Vector3.Dot(relativePosition, transform.forward); // Forward vector of the trigger

        // Apply threshold check for vertical movement
        if (Mathf.Abs(forwardDot) > directionThreshold)
        {
            return forwardDot > 0 ? transform.forward : -transform.forward;
        }

        // If the direction is not significant enough, no movement occurs
        return Vector3.zero;
    }

    private void MoveFocusObject(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        // Move the focus object vertically (z-axis)
        focusObject.position += new Vector3(0, 0, direction.z * verticalMoveAmount);
    }
}
