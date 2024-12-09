using UnityEngine;
using System.Collections;

public class VehicleControllerWithGears : MonoBehaviour
{
    public float[] gearAcceleration = { 10f, 15f, 20f, 25f, 30f, 5f }; // Acceleration for each gear including reverse gear
    public float[] gearSteering = { 4f, 3.5f, 3f, 2.5f, 2f, 2f };      // Steering sensitivity for each gear including reverse gear
    public float[] gearSpeedLimits = { 10f, 20f, 30f, 40f, 50f };      // Speed limit for each forward gear
    public float reverseSpeedLimit = 10f;                              // Speed limit for reverse gear

    public float maxSpeed = 50f;               // Max speed of the vehicle (forward)
    public float accelerationRate = 5f;        // Rate at which the acceleration ramps up
    public float baseDecelerationRate = 5f;    // Base rate of deceleration
    public float maxDecelerationRate = 20f;    // Maximum deceleration rate
    public float horizontalDrag = 0.98f;       // Simulate horizontal friction and resistance
    public float horizontalXDrag = 0.94f;
    public float horizontalZDrag = 0.94f;
    public float verticalDrag = 0.99f;         // Simulate vertical friction and resistance
    public float currentSpeed = 0f;            // Variable to store current speed
    public int currentGear = 0;               // Start at gear 1 (index 0)

    public float curveAmount = 1.0f;
    public float raycastDistance = 10f; // Distance for each raycast
    public LayerMask detectionLayer;

    private Rigidbody rb;
    private float moveInput;
    public float steeringInput;
    public float currentAcceleration = 0f;    // The current acceleration applied (ramps up)

    private float decelerationTime = 0f;        // Timer for how long S is pressed
    public float maxDecelerationTime = 5f;

    public bool isSKeyReleased = true;         // Tracks if the S key has been released

    // Add GameObjects for blinkers
    public GameObject leftBlinker;
    public GameObject rightBlinker;

    // Variables to handle blinking state
    private Coroutine leftBlinkerCoroutine;
    private Coroutine rightBlinkerCoroutine;
    private bool isLeftBlinkerOn = false;
    private bool isRightBlinkerOn = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Turn off blinkers initially
        SetBlinker(leftBlinker, false);
        SetBlinker(rightBlinker, false);
    }

    void Update()
    {
        // Get input from player (WASD / Arrow Keys)
        moveInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");

        //Debug.Log(currentGear);

        // Detect if S key has been released
        if (moveInput >= -0.1f)
        {
            isSKeyReleased = true;
        }

        // Check for blinker input
        HandleBlinkers();

        // Shift gears based on current speed
        UpdateGear();

        CheckForObstaclesAlongCurve();
    }

    public bool isDecelerating = false;  // Track if the vehicle is currently decelerating

    void FixedUpdate()
    {
        if (moveInput > 0.1f)
        {
            // Forward movement
            currentAcceleration = Mathf.MoveTowards(currentAcceleration, gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);
            isSKeyReleased = true; // Allow reversing again after moving forward
            decelerationTime = 0f; // Reset deceleration timer
            isDecelerating = false; // Reset deceleration state
        }
        else if (moveInput < -0.1f)
        {
            if (rb.velocity.magnitude < 0.1f)
            {
                // Begin reversing when stationary
                currentGear = gearAcceleration.Length - 1; // Switch to reverse gear
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, -gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);
            }
            else if (currentGear == gearAcceleration.Length - 1) // Reverse gear
            {
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, -gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);
            }
            else
            {
                // Apply braking to decelerate to stop
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, 0f, baseDecelerationRate * 2.0f * Time.fixedDeltaTime);
                isDecelerating = true;
            }
        }
        else
        {
            // Neutral state (no input)
            currentAcceleration = Mathf.MoveTowards(currentAcceleration, 0f, baseDecelerationRate * 0.5f * Time.fixedDeltaTime);
            isDecelerating = true;
        }

        // Apply force for movement
        Vector3 force = transform.forward * currentAcceleration;
        rb.AddForce(force);

        // Limit speed based on gear
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (currentGear < gearAcceleration.Length - 1)
        {
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        }
        else
        {
            velocity = Vector3.ClampMagnitude(velocity, reverseSpeedLimit);
        }
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

        // Update current speed
        currentSpeed = velocity.magnitude;

        // Apply drag and dampening
        ApplyDragAndDampening();

        // Handle steering
        HandleSteering();

        // Update gear based on speed
        UpdateGear();
    }

    void UpdateGear()
    {
        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        bool isMovingForward = Vector3.Dot(rb.velocity, transform.forward) > 0;

        if (moveInput > 0.1f && currentGear == gearAcceleration.Length - 1 && isMovingForward)
        {
            // Switch to first gear if moving forward from reverse
            currentGear = 0;
        }
        else if (moveInput < -0.1f && isSKeyReleased && currentGear != gearAcceleration.Length - 1 && !isMovingForward)
        {
            // Switch to reverse gear if moving backward
            currentGear = gearAcceleration.Length - 1;
        }
        else if (isMovingForward)
        {
            // Handle forward gear shifting
            if (currentGear < gearSpeedLimits.Length && speed > gearSpeedLimits[currentGear])
            {
                currentGear++;
            }
            else if (currentGear > 0 && speed < gearSpeedLimits[currentGear - 1])
            {
                currentGear--;
            }
        }
        else
        {
            // Safeguard: Reset to reverse gear if moving backward
            if (currentGear < gearAcceleration.Length - 1 && speed < gearSpeedLimits[0])
            {
                currentGear = gearAcceleration.Length - 1;
            }
        }
    }

    void ApplyDragAndDampening()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        horizontalVelocity.x *= horizontalXDrag;
        horizontalVelocity.z *= horizontalZDrag;
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);

        float verticalVelocity = rb.velocity.y * verticalDrag;
        rb.velocity = new Vector3(rb.velocity.x, verticalVelocity, rb.velocity.z);

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x *= 0.99f;
        localVelocity.z *= 0.99f;
        rb.velocity = transform.TransformDirection(localVelocity);

        Vector3 angularVelocity = rb.angularVelocity * 0.9f;
        rb.angularVelocity = angularVelocity;
    }

    void HandleSteering()
    {
        if (Mathf.Abs(steeringInput) > 0.1f)
        {
            // Determine if the car is actually reversing
            bool isReversing = Vector3.Dot(rb.velocity, transform.forward) < 0;

            float turnInput = steeringInput;

            // Reverse steering direction only when actually reversing
            if (currentGear == gearAcceleration.Length - 1 && isReversing)
            {
                turnInput = -steeringInput;
            }

            float turn = turnInput * gearSteering[currentGear] * rb.velocity.magnitude * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }
    }


    void HandleBlinkers()
    {
        // Left Arrow blinker
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (leftBlinkerCoroutine != null) StopCoroutine(leftBlinkerCoroutine); // Stop previous coroutine if running
            isLeftBlinkerOn = !isLeftBlinkerOn;
            if (isLeftBlinkerOn)
            {
                leftBlinkerCoroutine = StartCoroutine(BlinkerCoroutine(leftBlinker));
            }
            else
            {
                SetBlinker(leftBlinker, false);
            }
            if (isRightBlinkerOn)
            {
                if (rightBlinkerCoroutine != null) StopCoroutine(rightBlinkerCoroutine); // Stop previous coroutine if running
                isRightBlinkerOn = !isRightBlinkerOn;
                SetBlinker(rightBlinker, false);
            }
        }

        // Right Arrow blinker
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (rightBlinkerCoroutine != null) StopCoroutine(rightBlinkerCoroutine); // Stop previous coroutine if running
            isRightBlinkerOn = !isRightBlinkerOn;
            if (isRightBlinkerOn)
            {
                rightBlinkerCoroutine = StartCoroutine(BlinkerCoroutine(rightBlinker));
            }
            else
            {
                SetBlinker(rightBlinker, false);
            }
            if (isLeftBlinkerOn)
            {
                if (leftBlinkerCoroutine != null) StopCoroutine(leftBlinkerCoroutine); // Stop previous coroutine if running
                isLeftBlinkerOn = !isLeftBlinkerOn;
                SetBlinker(leftBlinker, false);
            }
        }
    }

    void CheckForObstaclesAlongCurve()
    {
        // Starting position of the raycast (same height as the vehicle)
        Vector3 startPosition = transform.position; // No vertical offset

        // Calculate the forward direction and adjust it based on steering
        Vector3 forwardDirection = transform.forward;

        // Calculate the angle of curvature based on steering input
        float turnAngle = steeringInput * curveAmount; // Steering affects curvature

        // Calculate the position of the raycast point along the path
        Vector3 raycastPoint = startPosition + forwardDirection * (currentSpeed * Time.deltaTime);

        // Determine the raycast distance based on current speed
        float raycastDistance = Mathf.Clamp(currentSpeed, 0.5f, 1.7f); // Set min and max distance based on speed

        // Apply a rotation based on the turn angle
        raycastPoint = Quaternion.Euler(0, turnAngle * 90f, 0) * forwardDirection * raycastDistance + startPosition;

        // Perform the raycast
        if (Physics.Raycast(startPosition, raycastPoint - startPosition, raycastDistance, detectionLayer))
        {
            Debug.DrawRay(startPosition, raycastPoint - startPosition, Color.red);
            // Implement any logic for what happens when an obstacle is detected
        }
        else
        {
            Debug.DrawRay(startPosition, raycastPoint - startPosition, Color.green);
        }
    }


    // Coroutine to make a blinker blink
    IEnumerator BlinkerCoroutine(GameObject blinker)
    {
        while (true)
        {
            SetBlinker(blinker, true);   // Turn on blinker
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds
            SetBlinker(blinker, false);  // Turn off blinker
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds
        }
    }

    // Helper method to enable/disable blinker object
    void SetBlinker(GameObject blinker, bool isActive)
    {
        if (blinker != null)
        {
            blinker.SetActive(isActive);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Calculate the relative velocity at the point of collision
        float collisionImpact = collision.relativeVelocity.magnitude;

        // Determine speed reduction factor based on impact (e.g., stronger impact = greater reduction)
        float reductionFactor = Mathf.Clamp(collisionImpact / 2f, 0.1f, 1f);

        // Reduce current speed
        float reducedSpeed = currentSpeed * (1 - reductionFactor);
        currentSpeed = Mathf.Max(reducedSpeed, 0f); // Ensure speed doesn't go negative

        float reducedAcceleration = currentAcceleration * (1 - reductionFactor);
        currentAcceleration = Mathf.Max(reducedAcceleration, 0f);

        // Optional: Update the Rigidbody's velocity to reflect the reduced speed
        Vector3 velocity = rb.velocity.normalized * currentSpeed;
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

        // Optional: Log or visualize the impact
        Debug.Log($"Collision detected with {collision.gameObject.name}. Impact: {collisionImpact}, Speed reduced by: {reductionFactor * 100}%.");
    }



    public int GetCurrentGear()
    {
        return currentGear;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public bool GetIsLeftBlinkerOn()
    {
        return isLeftBlinkerOn;
    }

    public bool GetIsRightBlinkerOn()
    {
        return isRightBlinkerOn;
    }

    public float GetAcceleration()
    {
        return currentAcceleration;
    }
}