using UnityEngine;

public class DynamicCurvedRaycast : MonoBehaviour
{
    public Transform car; // Reference to the car's transform
    public VehicleControllerWithGears vehicleController; // Reference to the vehicle controller script
    public int rayCount = 15; // Number of raycasts to form the arc
    public float arcAngle = 45f; // Angle spread of the arc
    public float minDistance = 5f; // Minimum length of the rays
    public float accelerationMultiplier = 2f; // Multiplier to scale the effect of acceleration
    public int curveSegments = 10; // Number of segments for each curved ray
    public float predictionTime = 1f; // Time ahead to predict the car's path

    public LayerMask detectionLayer; // Layer mask for raycast detection

    void Update()
    {
        // Calculate dynamicDistance based on currentAcceleration
        float dynamicDistance = minDistance + vehicleController.currentAcceleration * accelerationMultiplier;

        float halfAngle = arcAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            // Calculate the angle for each ray relative to the car's current rotation
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (rayCount - 1));
            Vector3 initialDirection = Quaternion.Euler(0, angle, 0) * car.forward;

            // Simulate where the car would be after `predictionTime` seconds based on current speed and rotation
            Vector3 predictedCarPosition = car.position + initialDirection * (vehicleController.currentSpeed * predictionTime);
            Quaternion predictedCarRotation = Quaternion.Euler(0, vehicleController.steeringInput * vehicleController.gearSteering[vehicleController.currentGear] * predictionTime, 0);

            // Define the control point for the Bézier curve using the predicted position, on the XZ plane
            Vector3 controlPoint = (car.position + predictedCarPosition) / 2;
            controlPoint += predictedCarRotation * Vector3.right * (dynamicDistance / 4); // Curve along the Y-axis direction

            Vector3 previousPoint = car.position;

            // Iterate over the segments to create the curve
            for (int j = 1; j <= curveSegments; j++)
            {
                float t = j / (float)curveSegments;
                Vector3 currentPoint = CalculateBezierPoint(t, car.position, controlPoint, predictedCarPosition);

                // Cast a small ray between the previous and current points
                RaycastHit hit;
                if (Physics.Linecast(previousPoint, currentPoint, out hit, detectionLayer))
                {
                    Debug.DrawLine(previousPoint, hit.point, Color.red);
                    // Add logic here if you need to act on a hit
                    break; // Stop further casting if an obstacle is hit
                }
                else
                {
                    Debug.DrawLine(previousPoint, currentPoint, Color.green);
                }

                previousPoint = currentPoint; // Move to the next segment
            }
        }
    }

    // Function to calculate a point on a quadratic Bézier curve
    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0; // (1-t)^2 * p0
        point += 2 * u * t * p1; // 2(1-t)t * p1
        point += tt * p2;        // t^2 * p2

        return point;
    }
}
