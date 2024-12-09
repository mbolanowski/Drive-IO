using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    // Movement increments
    public float horizontalStep = 19.3f;
    public float verticalStep = 11.2f;

    // Methods to move the object
    public void MoveLeft()
    {
        transform.position += Vector3.left * horizontalStep;
    }

    public void MoveRight()
    {
        transform.position += Vector3.right * horizontalStep;
    }

    public void MoveUp()
    {
        transform.position += Vector3.forward * verticalStep;
    }

    public void MoveDown()
    {
        transform.position += Vector3.back * verticalStep;
    }
}
