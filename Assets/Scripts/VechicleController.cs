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
    public float verticalDrag = 0.99f;         // Simulate vertical friction and resistance
    public float currentSpeed = 0f;            // Variable to store current speed
    private int currentGear = 0;               // Start at gear 1 (index 0)

    private Rigidbody rb;
    private float moveInput;
    private float steeringInput;
    private float currentAcceleration = 0f;    // The current acceleration applied (ramps up)

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
    }

    public bool isDecelerating = false;  // Track if the vehicle is currently decelerating

    void FixedUpdate()
    {
        // Gradually increase the acceleration to make it ramp up smoothly when W is pressed
        if (moveInput > 0.1f)
        {
            // Increase acceleration when pressing W
            currentAcceleration = Mathf.MoveTowards(currentAcceleration, gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);
            decelerationTime = 0f; // Reset deceleration timer when accelerating
            isDecelerating = false; // Reset deceleration state
            isSKeyReleased = true; // Allow reversing again
        }
        else if (moveInput < -0.1f)
        {
            if (currentGear == gearAcceleration.Length - 1) // Reverse gear
            {
                if (!isSKeyReleased)
                {
                    currentAcceleration = 0f;
                }
                // Only allow reversing if the S key has been released after braking
                if (isSKeyReleased)
                {
                    // Apply reversing logic
                    currentAcceleration = Mathf.MoveTowards(currentAcceleration, -gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);
                    isDecelerating = false; // Reset deceleration state
                }
            }
            else if (currentGear >= 0 && currentGear < gearAcceleration.Length - 1) // Forward gears
            {
                // Set deceleration state to true when in forward gears
                isDecelerating = true;

                // Increase deceleration time while S is pressed
                decelerationTime += Time.fixedDeltaTime;

                // Calculate the current deceleration rate based on how long S has been pressed
                float currentDecelerationRate = Mathf.Lerp(baseDecelerationRate, maxDecelerationRate, decelerationTime / maxDecelerationTime); // 5 seconds max

                // Decrease acceleration based on the current deceleration rate
                currentAcceleration = Mathf.MoveTowards(currentAcceleration, -gearAcceleration[currentGear], currentDecelerationRate * Time.fixedDeltaTime);

                // Mark that the S key has not been released yet
                isSKeyReleased = false; // Mark that the S key has not been released yet
            }
        }

        // Apply the acceleration to move the vehicle forward or backward
        Vector3 force = transform.forward * currentAcceleration;
        rb.AddForce(force);

        // Limit the speed of the vehicle
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Ignore Y-axis velocity
        if (currentGear < gearAcceleration.Length - 1)
        {
            // Forward gears
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        }
        else
        {
            // Reverse gear
            velocity = Vector3.ClampMagnitude(velocity, reverseSpeedLimit);
        }
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z); // Apply new velocity

        // Update the current speed (magnitude of velocity in XZ plane)
        currentSpeed = velocity.magnitude;

        // Reset the current acceleration if the speed is 0 while holding S
        if (currentSpeed <= 0f && moveInput < -0.1f && currentGear != gearAcceleration.Length - 1)
        {
            currentAcceleration = 0f; // Reset acceleration when speed reaches 0 and S is pressed in forward gear
        }

        // Apply horizontal drag (X and Z components)
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        horizontalVelocity *= horizontalDrag; // Apply horizontal drag
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z); // Update horizontal velocity

        // Apply vertical drag (Y component)
        float verticalVelocity = rb.velocity.y * verticalDrag; // Apply vertical drag
        rb.velocity = new Vector3(rb.velocity.x, verticalVelocity, rb.velocity.z); // Update vertical velocity

        // Steering (rotate the vehicle based on input and speed)
        if (Mathf.Abs(steeringInput) > 0.1f)
        {
            // Adjust the rotation around Y-axis for steering based on the current gear
            float turnInput = steeringInput;

            // Reverse steering input when in reverse gear
            if (currentGear == gearAcceleration.Length - 1) // Check if in reverse gear
            {
                turnInput = -steeringInput; // Invert the steering input
            }

            float turn = turnInput * gearSteering[currentGear] * rb.velocity.magnitude * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        // Update gear based on speed and input
        UpdateGear();
    }

    // Update the current gear based on speed and input
    void UpdateGear()
    {
        // Get the vehicle's speed in XZ plane
        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;

        // Determine if moving forward or backward
        bool isMovingForward = Vector3.Dot(rb.velocity, transform.forward) > 0;

        // If moving forward, handle gear shifting for forward gears
        if (isMovingForward)
        {
            // Shift up gears based on speed (ensure we don't go beyond the last forward gear)
            if (currentGear < gearSpeedLimits.Length && speed > gearSpeedLimits[currentGear] && currentGear < gearSpeedLimits.Length - 1)
            {
                currentGear++;
            }

            // Shift down gears if speed drops (ensure we don't access below the first gear)
            if (currentGear > 0 && speed < gearSpeedLimits[currentGear - 1])
            {
                currentGear--;
            }

            // If moving forward but in reverse gear, switch to the first forward gear
            if (currentGear == gearAcceleration.Length - 1)
            {
                currentGear = 0;
            }
        }
        // If moving backward or starting to move backward, switch to reverse gear
        else
        {
            if (moveInput < -0.1f && currentGear != gearAcceleration.Length - 1)
            {
                currentGear = gearAcceleration.Length - 1; // Reverse gear
            }

            // Safeguard: If moving backward but in a forward gear, switch to reverse gear
            if (currentGear < gearAcceleration.Length - 1 && speed < gearSpeedLimits[0])
            {
                currentGear = gearAcceleration.Length - 1; // Reverse gear
            }
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
            if(isRightBlinkerOn)
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
}