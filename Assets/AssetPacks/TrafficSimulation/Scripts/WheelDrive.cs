using UnityEngine;
using System;

namespace TrafficSimulation
{
    [Serializable]
    public enum DriveType
    {
        RearWheelDrive,
        FrontWheelDrive,
        AllWheelDrive
    }

    [Serializable]
    public enum UnitType
    {
        KMH,
        MPH
    }

    public class WheelDrive : MonoBehaviour
    {
        [Tooltip("Downforce applied to the vehicle")]
        public float downForce = 100f;

        [Tooltip("Maximum steering angle (degrees) per second")]
        public float maxSteeringAngle = 30f;

        [Tooltip("Speed at which we interpolate steering (lerp)")]
        public float steeringLerp = 5f;

        [Tooltip("Max speed (in the unit chosen below) when the vehicle is about to steer")]
        public float steeringSpeedMax = 20f;

        [Tooltip("Minimum speed in the chosen unit")]
        public float minSpeed = 5f;

        [Tooltip("Maximum speed in the chosen unit")]
        public float maxSpeed = 50f;

        [Tooltip("Acceleration force applied to the vehicle")]
        public float accelerationForce = 1000f;

        [Tooltip("Braking force applied to the vehicle")]
        public float brakeForce = 500f;

        [Tooltip("Lateral stability factor (higher = less sliding)")]
        public float lateralStabilityFactor = 5f;

        [Tooltip("Unit Type")]
        public UnitType unitType;

        private Rigidbody rb;
        private float currentSteering = 0f;

        public float currentSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false; // Disable gravity for "air" driving
        }

        public void Move(float _acceleration, float _steering, float _brake, Status currentStatus)
        {
            currentSpeed = GetSpeedUnit(rb.velocity.magnitude);

            // Steering effectiveness based on speed
            float steeringEffectiveness = Mathf.Clamp01(steeringSpeedMax / currentSpeed);
            float targetSteering = _steering * maxSteeringAngle * steeringEffectiveness;
            currentSteering = Mathf.Lerp(currentSteering, targetSteering, Time.deltaTime * steeringLerp);

            Quaternion steerRotation = Quaternion.Euler(0, currentSteering * Time.deltaTime, 0);
            rb.MoveRotation(rb.rotation * steerRotation);

            // Acceleration logic
            if (_acceleration > 0)
            {
                Vector3 forwardForce = transform.forward * (_acceleration * accelerationForce);
                rb.AddForce(forwardForce, ForceMode.Acceleration);
            }

            // Enforce minimum speed
            if (currentStatus != Status.STOP && currentSpeed < minSpeed && _acceleration > 0)
            {
                float minSpeedForce = GetSpeedMS(minSpeed) - rb.velocity.magnitude;
                rb.AddForce(transform.forward * minSpeedForce, ForceMode.VelocityChange);
            }

            if (_brake > 0 && rb.velocity.magnitude > 0.1f) // Check if the car is moving
            {
                Vector3 brakeForceVector = -rb.velocity.normalized * (_brake * brakeForce);
                rb.AddForce(brakeForceVector, ForceMode.Acceleration);
            }
            else if(_brake > 0)
            {
                rb.velocity = Vector3.zero;
            }

            // Limit speed to maxSpeed
            if (currentSpeed > maxSpeed)
            {
                rb.velocity = GetSpeedMS(maxSpeed) * rb.velocity.normalized;
            }

            // Apply downforce for stability
            rb.AddForce(-transform.up * downForce * rb.velocity.magnitude);

            // Apply lateral stability
            ApplyLateralStabilization();
        }

        private void ApplyLateralStabilization()
        {
            // Get the vehicle's velocity in local space
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
            
            if (GetSpeedUnit(rb.velocity.magnitude) > 0.1f) // Only apply stabilization when moving
            {
                Vector3 lateralForce = -transform.right * (localVelocity.x * lateralStabilityFactor);
                rb.AddForce(lateralForce, ForceMode.Acceleration);
            }
            // Align the velocity with the forward direction of the vehicle
            Vector3 alignedVelocity = transform.forward * localVelocity.z;
            rb.velocity = alignedVelocity + (transform.right * localVelocity.x * 0.1f); // Keep minimal lateral velocity for realism
        }

        public float GetSpeedMS(float _s)
        {
            return unitType == UnitType.KMH ? _s / 3.6f : _s / 2.237f;
        }

        public float GetSpeedUnit(float _s)
        {
            return unitType == UnitType.KMH ? _s * 3.6f : _s * 2.237f;
        }
    }
}
