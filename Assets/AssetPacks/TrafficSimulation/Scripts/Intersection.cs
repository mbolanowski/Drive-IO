using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public enum IntersectionType
    {
        STOP,
        TRAFFIC_LIGHT
    }

    public class Intersection : MonoBehaviour
    {
        // Previous fields remain the same...
        public IntersectionType intersectionType;
        public int id;
        public float lightsDuration = 8;
        public float orangeLightDuration = 2;
        public List<Segment> lightsNbr1;
        public List<Segment> lightsNbr2;

        private List<GameObject> turningLeftQueue;
        private List<GameObject> turningRightQueue;
        private List<GameObject> turningStraightQueue;
        private List<GameObject> vehiclesInIntersection;
        private TrafficSystem trafficSystem;

        [HideInInspector] public int currentRedLightsGroup = 1;

        int num = 0;

        void Start()
        {
            turningLeftQueue = new List<GameObject>();
            turningRightQueue = new List<GameObject>();
            turningStraightQueue = new List<GameObject>();
            vehiclesInIntersection = new List<GameObject>();

            if (intersectionType == IntersectionType.TRAFFIC_LIGHT)
                InvokeRepeating("SwitchLights", lightsDuration, lightsDuration);
        }
        
        bool IsVehicleToTheRight(Transform straightVehicle, Transform rightTurningVehicle)
        {
            // Get positions relative to intersection center
            Vector3 intersectionCenter = transform.position;
            Vector3 straightRelative = straightVehicle.position - intersectionCenter;
            Vector3 rightTurnerRelative = rightTurningVehicle.position - intersectionCenter;

            // For a vehicle going straight, check if the right-turning vehicle is actually to its right
            // by using cross product - if it's positive, the right-turning vehicle is to the left
            float crossProduct = Vector3.Cross(straightRelative.normalized, rightTurnerRelative.normalized).y;

            // If cross product is positive, rightTurner is to the left of straight vehicle
            // If negative, rightTurner is to the right of straight vehicle
            return crossProduct < 0;
        }

        bool WillPathsIntersect(GameObject vehicle1, GameObject vehicle2)
        {
            VehicleAI ai1 = vehicle1.GetComponent<VehicleAI>();
            VehicleAI ai2 = vehicle2.GetComponent<VehicleAI>();

            // If one is going straight and other is turning right
            if (ai1._TurningStraight && ai2._TurningRight)
            {
                return IsVehicleToTheRight(vehicle1.transform, vehicle2.transform);
            }
            else if (ai2._TurningStraight && ai1._TurningRight)
            {
                return ai2.vehicleStatus != Status.STOP && IsVehicleToTheRight(vehicle2.transform, vehicle1.transform);
            }

            // Handle other cases (left turns etc) as before using horizontal/vertical checks
            if (ai1._isHorizontal == ai2._isHorizontal)
            {
                return (ai1._TurningLeft && ai2._TurningStraight) ||
                       (ai1._TurningLeft && ai2._TurningRight)    ||
                       (ai1._TurningLeft && ai2._TurningLeft) ||
                       (ai2.vehicleStatus != Status.STOP && (ai1._TurningRight && ai2._TurningLeft)) ||
                       (ai2.vehicleStatus != Status.STOP && (ai1._TurningStraight && ai2._TurningLeft));
            }
            else
            {
                return (ai1._TurningLeft && ai2._TurningStraight) ||
                       (ai1._TurningLeft && ai2._TurningLeft) ||
                       (ai1._TurningStraight && ai2._TurningStraight) ||
                       (ai2.vehicleStatus != Status.STOP && (ai1._TurningStraight && ai2._TurningLeft));
            }
        }

        bool HasConflictingVehicleInIntersection(GameObject vehicle)
        {
            /*VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();

            foreach (GameObject otherVehicle in vehiclesInIntersection)
            {
                if(otherVehicle.GetComponent<VehicleAI>()._TurningLeft && vehicleAI._TurningLeft)
                {
                    return false;
                }
                if (WillPathsIntersect(vehicle, otherVehicle))
                {
                    return true;
                }
            }*/
            return false;
        }

        bool IsFromSameEntrance(GameObject vehicle1, GameObject vehicle2)
        {
            VehicleAI ai1 = vehicle1.GetComponent<VehicleAI>();
            VehicleAI ai2 = vehicle2.GetComponent<VehicleAI>();

            int entrance1 = ai1.intersectionEntranceDirection;
            int entrance2 = ai2.intersectionEntranceDirection;

            return entrance1 == entrance2;
        }

        bool HasHigherPriorityVehicleInQueue(GameObject vehicle)
        {
            VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();
            int vehicleEntrance = vehicleAI.intersectionEntranceDirection;
            bool isFromPrioritySegment = vehicleAI.hasPriority;

            // Function to check if a vehicle should yield to another based on priority
            bool ShouldYieldTo(GameObject otherVehicle)
            {
                if (otherVehicle == vehicle) return false;

                VehicleAI otherAI = otherVehicle.GetComponent<VehicleAI>();
                bool otherHasPriority = otherAI.hasPriority;

                // If this vehicle is from a priority segment and the other isn't, don't yield
                if (isFromPrioritySegment && !otherHasPriority) return false;

                // If this vehicle isn't from a priority segment but the other is, yield
                if (!isFromPrioritySegment && otherHasPriority) return true;

                // If both have same priority status, use normal intersection rules
                int otherEntrance = otherAI.intersectionEntranceDirection;
                if (otherEntrance != vehicleEntrance)
                {
                    return WillPathsIntersect(vehicle, otherVehicle);
                }

                return false;
            }

            // Check all queues in order of normal priority
            if (turningRightQueue.Count > 0)
            {
                GameObject firstRight = turningRightQueue[0];
                if (ShouldYieldTo(firstRight)) return true;
            }

            if (turningStraightQueue.Count > 0)
            {
                GameObject firstStraight = turningStraightQueue[0];
                if (ShouldYieldTo(firstStraight)) return true;
            }

            if (turningLeftQueue.Count > 0)
            {
                GameObject firstLeft = turningLeftQueue[0];
                if (ShouldYieldTo(firstLeft)) return true;
            }

            return false;
        }

        bool CanVehicleProceed(GameObject vehicle)
        {
            VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();
            bool isFromPrioritySegment = vehicleAI.hasPriority;

            // Always check for actual collisions with vehicles in intersection, regardless of priority
            foreach (GameObject otherVehicle in vehiclesInIntersection)
            {
                if (!IsFromSameEntrance(vehicle, otherVehicle) && WillPathsIntersect(vehicle, otherVehicle))
                {
                    return false;
                }
            }

            // Check queue priority
            if (HasHigherPriorityVehicleInQueue(vehicle))
            {
                return false;
            }

            // Additional checks based on turning direction
            if (vehicleAI._TurningLeft)
            {
                // For vehicles from priority segments, only check for physical conflicts
                if (isFromPrioritySegment)
                {
                    return !HasConflictingVehicleInIntersection(vehicle);
                }

                // For non-priority segments, use normal left turn rules
                bool hasOpposingStraightTraffic = false;
                foreach (GameObject straightVehicle in turningStraightQueue)
                {
                    if (!IsFromSameEntrance(vehicle, straightVehicle))
                    {
                        VehicleAI straightAI = straightVehicle.GetComponent<VehicleAI>();
                        if (straightAI._isHorizontal == vehicleAI._isHorizontal)
                        {
                            hasOpposingStraightTraffic = true;
                            break;
                        }
                    }
                }

                return !hasOpposingStraightTraffic && !HasConflictingVehicleInIntersection(vehicle);
            }
            else if (vehicleAI._TurningRight)
            {
                // Even priority vehicles need to check for physical conflicts
                return !HasConflictingVehicleInIntersection(vehicle);
            }
            else if (vehicleAI._TurningStraight)
            {
                // Check for any vehicles turning right that would intersect our path
                bool hasConflictingRightTurn = false;
                if (turningRightQueue.Count > 0)
                {
                    GameObject rightTurningVehicle = turningRightQueue[0];
                    if (!IsFromSameEntrance(vehicle, rightTurningVehicle))
                    {
                        hasConflictingRightTurn = WillPathsIntersect(vehicle, rightTurningVehicle);
                    }
                }

                return !hasConflictingRightTurn && !HasConflictingVehicleInIntersection(vehicle);
            }

            return false;
        }

        void CheckQueuedVehicles()
        {
            // We'll check all vehicles in all queues
            // Multiple vehicles from the same entrance can proceed if they meet the criteria
            List<GameObject> vehiclesToMove = new List<GameObject>();

            void CheckQueue(List<GameObject> queue)
            {
                foreach (GameObject vehicle in queue)
                {
                    if (!vehiclesInIntersection.Contains(vehicle) && CanVehicleProceed(vehicle))
                    {
                        vehiclesToMove.Add(vehicle);
                    }
                }
            }

            // Check queues in priority order
            CheckQueue(turningRightQueue);
            CheckQueue(turningStraightQueue);
            CheckQueue(turningLeftQueue);


            // Move all approved vehicles
            foreach (GameObject vehicle in vehiclesToMove)
            {
                vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.SLOW_DOWN;
                vehiclesInIntersection.Add(vehicle);
            }
        }

        // Rest of the methods remain the same as in previous implementation...
        void OnTriggerEnter(Collider _other)
        {
            //if (IsAlreadyInIntersection(_other.gameObject) || Time.timeSinceLevelLoad < .5f) return;

            if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.STOP)
                TriggerStop(_other.gameObject);
            else if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.TRAFFIC_LIGHT)
                TriggerLight(_other.gameObject);
        }

        void OnTriggerExit(Collider _other)
        {
            if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.STOP)
                ExitStop(_other.gameObject);
            else if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.TRAFFIC_LIGHT)
                ExitLight(_other.gameObject);
        }

        void TriggerStop(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();

            // Add vehicle to appropriate queue based on turning direction
            if (vehicleAI._TurningLeft)
            {
                turningLeftQueue.Add(_vehicle);
            }
            else if (vehicleAI._TurningRight)
            {
                turningRightQueue.Add(_vehicle);
            }
            else if (vehicleAI._TurningStraight)
            {
                turningStraightQueue.Add(_vehicle);
            }


            // Check if vehicle can proceed immediately
            if (CanVehicleProceed(_vehicle))
            {
                vehicleAI.vehicleStatus = Status.SLOW_DOWN;
                vehiclesInIntersection.Add(_vehicle);
            }
            else
            {
                vehicleAI.vehicleStatus = Status.STOP;
            }
        }

        void ExitStop(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();

            // Remove from appropriate queue
            if (vehicleAI._TurningLeft)
            {
                turningLeftQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }
            else if (vehicleAI._TurningRight)
            {
                turningRightQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }
            else if (vehicleAI._TurningStraight)
            {
                turningStraightQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }

            vehicleAI.vehicleStatus = Status.GO;
            vehiclesInIntersection.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());

            // Reset turning flags
            vehicleAI._TurningLeft = false;
            vehicleAI._TurningRight = false;
            vehicleAI._TurningStraight = false;

            // Check if any waiting vehicles can now proceed
            CheckQueuedVehicles();
        }

        bool IsVehicleQueuedFirst(GameObject vehicle1, GameObject vehicle2)
        {
            // Check each queue in order to find which vehicle appears first
            foreach (GameObject queuedVehicle in turningLeftQueue.Concat(turningRightQueue).Concat(turningStraightQueue))
            {
                if (queuedVehicle == vehicle1) return true;
                if (queuedVehicle == vehicle2) return false;
            }
            return false; // Shouldn't reach here if both vehicles are in queues
        }


        bool WillPathsIntersectTrafficLight(GameObject vehicle1, GameObject vehicle2)
        {
            VehicleAI ai1 = vehicle1.GetComponent<VehicleAI>();
            VehicleAI ai2 = vehicle2.GetComponent<VehicleAI>();

            // If one is going straight and other is turning right
            if (ai1._TurningStraight && ai2._TurningRight)
            {
                return IsVehicleToTheRight(vehicle1.transform, vehicle2.transform);
            }
            else if (ai2._TurningStraight && ai1._TurningRight)
            {
                return ai2.vehicleStatus != Status.STOP && IsVehicleToTheRight(vehicle2.transform, vehicle1.transform);
            }

            if (IsVehicleQueuedFirst(vehicle1, vehicle2))
            {
                return false;
            }

            if (ai1._isHorizontal == ai2._isHorizontal)
            {
                return (ai1._TurningLeft && ai2._TurningStraight) ||
                        (ai1._TurningLeft && ai2._TurningRight) ||
                        (ai1._TurningLeft && ai2._TurningLeft) ||
                        ((ai2.vehicleStatus != Status.STOP || ai2.vehicleStatus != Status.GO) && (ai1._TurningRight && ai2._TurningLeft)) ||
                        ((ai2.vehicleStatus != Status.STOP || ai2.vehicleStatus != Status.GO) && (ai1._TurningStraight && ai2._TurningLeft));
            }
            else
            {
                return (ai1._TurningLeft && ai2._TurningStraight) ||
                        (ai1._TurningLeft && ai2._TurningLeft) ||
                        (ai1._TurningStraight && ai2._TurningStraight) ||
                        ((ai2.vehicleStatus != Status.STOP || ai2.vehicleStatus != Status.GO) && (ai1._TurningStraight && ai2._TurningLeft));
            }
        }

        bool IsPlayerInVehiclePath(GameObject vehicle)
        {
            VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();

            // Assuming the vehicle has a forward-facing raycast
            RaycastHit hit;
            float rayDistance = 10f; // Adjust based on your needs
            int playerLayerMask = 1 << LayerMask.NameToLayer("Player");

            if (Physics.Raycast(vehicle.transform.position, vehicle.transform.forward, out hit, rayDistance, playerLayerMask))
            {
                return true;
            }

            return false;
        }

        bool CanVehicleProceedTrafficLight(GameObject vehicle)
        {
            VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();
            int vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

            // If red light, vehicle cannot proceed under any circumstances
            if (IsRedLightSegment(vehicleSegment))
            {
                return false;
            }

            // Check if there's a player in the vehicle's path
            if (IsPlayerInVehiclePath(vehicle))
            {
                return false;
            }

            // Always check for actual conflicts with vehicles in intersection
            foreach (GameObject otherVehicle in vehiclesInIntersection)
            {
                if (otherVehicle.GetComponent<VehicleAI>().vehicleStatus == Status.STOP)
                {
                    if (!IsFromSameEntrance(vehicle, otherVehicle) && WillPathsIntersectTrafficLight(vehicle, otherVehicle))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!IsFromSameEntrance(vehicle, otherVehicle) && WillPathsIntersect(vehicle, otherVehicle))
                    {
                        return false;
                    }
                }
            }

            // For green lights, still respect right-hand rule with other green light vehicles
            foreach (GameObject otherVehicle in turningRightQueue.Concat(turningStraightQueue).Concat(turningLeftQueue))
            {
                if (otherVehicle == vehicle) continue;

                VehicleAI otherAI = otherVehicle.GetComponent<VehicleAI>();
                int otherSegment = otherAI.GetSegmentVehicleIsIn();

                // Only check priority against other vehicles that also have a green light
                if (!IsRedLightSegment(otherSegment) && !IsFromSameEntrance(vehicle, otherVehicle))
                {
                    if (WillPathsIntersectTrafficLight(vehicle, otherVehicle))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        void TriggerLight(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();

            //Debug.Log("ADDED " + _vehicle.name);
            // Add vehicle to appropriate queue based on turning direction
            if (vehicleAI._TurningLeft)
            {
                turningLeftQueue.Add(_vehicle);
            }
            else if (vehicleAI._TurningRight)
            {
                turningRightQueue.Add(_vehicle);
            }
            else if (vehicleAI._TurningStraight)
            {
                turningStraightQueue.Add(_vehicle);
            }

            // Check if vehicle can proceed
            if (CanVehicleProceedTrafficLight(_vehicle))
            {
                vehicleAI.vehicleStatus = Status.SLOW_DOWN;
                vehiclesInIntersection.Add(_vehicle);
                //CheckQueuedVehiclesTrafficLight();
            }
            else
            {
                vehicleAI.vehicleStatus = Status.STOP;
            }
        }

        void ExitLight(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();

            //Debug.Log("LEFT " + _vehicle.name);

            // Remove from appropriate queue
            if (vehicleAI._TurningLeft)
            {
                turningLeftQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }
            else if (vehicleAI._TurningRight)
            {
                turningRightQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }
            else if (vehicleAI._TurningStraight)
            {
                turningStraightQueue.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());
            }

            vehicleAI.vehicleStatus = Status.GO;
            vehiclesInIntersection.RemoveAll(v => v.GetInstanceID() == _vehicle.GetInstanceID());

            // Reset turning flags
            vehicleAI._TurningLeft = false;
            vehicleAI._TurningRight = false;
            vehicleAI._TurningStraight = false;

            // Only check queued vehicles with green lights
            CheckQueuedVehiclesTrafficLight();
        }

        void CheckQueuedVehiclesTrafficLight()
        {
            List<GameObject> vehiclesToCheck = new List<GameObject>();

            // Collect all queued vehicles that have a green light
            foreach (GameObject vehicle in turningRightQueue.Concat(turningStraightQueue).Concat(turningLeftQueue))
            {
                VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();
                int vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

                if (!IsRedLightSegment(vehicleSegment))
                {
                    vehiclesToCheck.Add(vehicle);
                }
            }

            // Check if any green light vehicles can proceed
            foreach (GameObject vehicle in vehiclesToCheck)
            {
                if (CanVehicleProceedTrafficLight(vehicle))
                {
                    vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.SLOW_DOWN;
                    vehiclesInIntersection.Add(vehicle);
                }
            }
        }

        void SwitchLights()
        {
            // Switch light groups
            if (currentRedLightsGroup == 1) currentRedLightsGroup = 2;
            else if (currentRedLightsGroup == 2) currentRedLightsGroup = 1;

            // Wait for orange light duration before checking queued vehicles
            Invoke("CheckQueuedVehiclesTrafficLight", orangeLightDuration);
        }

        bool IsRedLightSegment(int _vehicleSegment)
        {
            if (currentRedLightsGroup == 1)
            {
                foreach (Segment segment in lightsNbr1)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            else
            {
                foreach (Segment segment in lightsNbr2)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            return false;
        }

        void MoveVehiclesQueue()
        {
            List<GameObject> vehiclesToMove = new List<GameObject>();

            foreach (GameObject vehicle in turningRightQueue)
            {
                int vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (!IsRedLightSegment(vehicleSegment))
                {
                    vehiclesToMove.Add(vehicle);
                }
            }

            foreach (GameObject vehicle in turningStraightQueue)
            {
                int vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (!IsRedLightSegment(vehicleSegment))
                {
                    vehiclesToMove.Add(vehicle);
                }
            }

            foreach (GameObject vehicle in turningLeftQueue)
            {
                int vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (!IsRedLightSegment(vehicleSegment))
                {
                    vehiclesToMove.Add(vehicle);
                }
            }

            foreach (GameObject vehicle in vehiclesToMove)
            {
                vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.SLOW_DOWN;
                turningLeftQueue.Remove(vehicle);
                turningRightQueue.Remove(vehicle);
                turningStraightQueue.Remove(vehicle);
            }
        }

        bool IsAlreadyInIntersection(GameObject _target)
        {
            if (vehiclesInIntersection.Contains(_target)) return true;
            if (turningLeftQueue.Contains(_target)) return true;
            if (turningRightQueue.Contains(_target)) return true;
            if (turningStraightQueue.Contains(_target)) return true;
            return false;
        }

        private List<GameObject> memTurningLeftQueue = new List<GameObject>();
        private List<GameObject> memTurningRightQueue = new List<GameObject>();
        private List<GameObject> memTurningStraightQueue = new List<GameObject>();
        private List<GameObject> memVehiclesInIntersection = new List<GameObject>();

        public void SaveIntersectionStatus()
        {
            memTurningLeftQueue = new List<GameObject>(turningLeftQueue);
            memTurningRightQueue = new List<GameObject>(turningRightQueue);
            memTurningStraightQueue = new List<GameObject>(turningStraightQueue);
            memVehiclesInIntersection = new List<GameObject>(vehiclesInIntersection);
        }

        public void ResumeIntersectionStatus()
        {
            foreach (GameObject v in vehiclesInIntersection)
            {
                foreach (GameObject v2 in memVehiclesInIntersection)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }

            void RestoreQueue(List<GameObject> currentQueue, List<GameObject> memQueue)
            {
                foreach (GameObject v in currentQueue)
                {
                    foreach (GameObject v2 in memQueue)
                    {
                        if (v.GetInstanceID() == v2.GetInstanceID())
                        {
                            v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                            break;
                        }
                    }
                }
            }

            RestoreQueue(turningLeftQueue, memTurningLeftQueue);
            RestoreQueue(turningRightQueue, memTurningRightQueue);
            RestoreQueue(turningStraightQueue, memTurningStraightQueue);
        }


        void Update()
        {
            if(this.gameObject.name == "Intersection-2")
            {
               foreach(GameObject carrr in vehiclesInIntersection)
                {
                    Debug.Log(carrr.name);
                }
            }
        }

    }
}