using UnityEngine;

public class VehicleControllerWithGears : MonoBehaviour
{
    public float[] gearAcceleration = { 10f, 15f, 20f, 25f, 30f }; // Acceleration for each gear
    public float[] gearSteering = { 4f, 3.5f, 3f, 2.5f, 2f };      // Steering sensitivity for each gear
    public float[] gearSpeedLimits = { 10f, 20f, 30f, 40f, 50f };  // Speed limit for each gear

    public float maxSpeed = 50f;               // Max speed of the vehicle
    public float accelerationRate = 5f;        // Rate at which the acceleration ramps up
    public float horizontalDrag = 0.98f;       // Simulate horizontal friction and resistance
    public float verticalDrag = 0.99f;         // Simulate vertical friction and resistance
    private int currentGear = 0;               // Start at gear 1 (index 0)

    private Rigidbody rb;
    private float moveInput;
    private float steeringInput;
    private float currentAcceleration = 0f;    // The current acceleration applied (ramps up)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input from player (WASD / Arrow Keys)
        moveInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");

        Debug.Log(currentGear);

        // Shift gears based on current speed
        UpdateGear();
    }

    void FixedUpdate()
    {
        // Gradually increase the acceleration to make it ramp up smoothly
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            // Increase acceleration over time
            currentAcceleration = Mathf.MoveTowards(currentAcceleration, gearAcceleration[currentGear], accelerationRate * Time.fixedDeltaTime);

            // Move the vehicle forward in the direction it's facing
            Vector3 force = transform.forward * currentAcceleration * moveInput;
            rb.AddForce(force);
        }
        else
        {
            // Reset the current acceleration when no input is provided
            currentAcceleration = 0f;
        }

        // Limit the speed of the vehicle (only in XZ plane)
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);  // Ignore Y-axis velocity
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);  // Apply new velocity

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
            float turn = steeringInput * gearSteering[currentGear] * rb.velocity.magnitude * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }
    }

    // Update the current gear based on speed
    void UpdateGear()
    {
        // Get the vehicle's speed in XZ plane
        float currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;

        // Shift up gears based on speed (ensure we don't go beyond the last gear)
        if (currentSpeed > gearSpeedLimits[currentGear] && currentGear < gearSpeedLimits.Length - 1)
        {
            currentGear++;
            // We do NOT reset currentAcceleration here
        }

        // Shift down gears if speed drops (ensure we don't access below the first gear)
        if (currentGear > 0 && currentSpeed < gearSpeedLimits[currentGear - 1])
        {
            currentGear--;
            // We do NOT reset currentAcceleration here
        }
    }
}