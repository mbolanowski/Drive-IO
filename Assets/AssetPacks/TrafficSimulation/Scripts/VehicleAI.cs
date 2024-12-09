// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TrafficSimulation {

    /*
        [-] Check prefab #6 issue
        [-] Deaccelerate when see stop in front
        [-] Smooth sharp turns when two segments are linked
        
    */

    public struct Target{
        public int segment;
        public int waypoint;
    }

    public enum Status{
        GO,
        STOP,
        SLOW_DOWN
    }

    public class VehicleAI : MonoBehaviour
    {
        [Header("Traffic System")]
        [Tooltip("Current active traffic system")]
        public TrafficSystem trafficSystem;

        [Tooltip("Determine when the vehicle has reached its target. Can be used to \"anticipate\" earlier the next waypoint (the higher this number his, the earlier it will anticipate the next waypoint)")]
        public float waypointThresh = 6;

        [Tooltip("Distance before checking for the next turn.")]
        public float distanceTresh = 6;

        [Tooltip("How much the car is planning to turn")]
        public float futureSteering = 0.0f;

        [Header("Radar")]

        [Tooltip("Empty gameobject from where the rays will be casted")]
        public Transform raycastAnchor;

        [Tooltip("Length of the casted rays")]
        public float raycastLength = 5;

        [Tooltip("Spacing between each rays")]
        public int raySpacing = 2;

        [Tooltip("Number of rays to be casted")]
        public int raysNumber = 6;

        [Tooltip("If detected vehicle is below this distance, ego vehicle will stop")]
        public float emergencyBrakeThresh = 2f;

        [Tooltip("If detected vehicle is below this distance at intersection, the vehicle will stop")]
        public float intersectionBrake = 2f;

        [Tooltip("If detected vehicle is below this distance (and above, above distance), ego vehicle will slow down")]
        public float slowDownThresh = 4f;

        private float timeSinceLastCheck = 0f;

        [SerializeField] public Status vehicleStatus = Status.GO;

        private WheelDrive wheelDrive;
        private CarsManager cm;
        private VechicleManager vm;
        private PlayerManager pm;
        private float initMaxSpeed = 0;
        private int pastTargetSegment = -1;
        private Target currentTarget;
        private Target futureTarget;

        public GameObject leftBlinker;
        public GameObject rightBlinker;
        // Variables to handle blinking state
        private Coroutine leftBlinkerCoroutine;
        private Coroutine rightBlinkerCoroutine;
        private bool isLeftBlinkerOn = false;
        private bool isRightBlinkerOn = false;

        void Start()
        {
            wheelDrive = this.GetComponent<WheelDrive>();

            cm = transform.parent.GetComponent<CarsManager>();
            vm = cm.vm;
            pm = cm.pm;

            if (trafficSystem == null)
            {
                trafficSystem = GameObject.Find("Traffic System").GetComponent<TrafficSystem>();
            }

            initMaxSpeed = wheelDrive.maxSpeed;
            SetWaypointVehicleIsOn();
        }

        void Update(){
            timeSinceLastCheck += Time.deltaTime;

            if (trafficSystem == null)
                return;

            WaypointChecker();
            HandleBlinkers();
            MoveVehicle();
        }


        void WaypointChecker(){
            GameObject waypoint = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].gameObject;

            //Position of next waypoint relative to the car
            Vector3 wpDist = this.transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x, this.transform.position.y, waypoint.transform.position.z));

            //Go to next waypoint if arrived to current
            if(wpDist.magnitude < waypointThresh){
                //Get next target
                currentTarget.waypoint++;
                if(currentTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                    pastTargetSegment = currentTarget.segment;
                    currentTarget.segment = futureTarget.segment;
                    currentTarget.waypoint = 0;
                }

                //Get future target
                futureTarget.waypoint = currentTarget.waypoint + 1;
                if(futureTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                    futureTarget.waypoint = 0;
                    futureTarget.segment = GetNextSegmentId();
                }
            }
        }

        void MoveVehicle(){

            //Default, full acceleration, no break and no steering
            float acc = 1;
            float brake = 0;
            float steering = 0;
            wheelDrive.maxSpeed = initMaxSpeed;

            //Calculate if there is a planned turn
            Transform targetTransform = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform;
            Transform futureTargetTransform = trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].transform;

            // Distance to the current waypoint
            float distanceToWaypoint = Vector3.Distance(this.transform.position, targetTransform.position);

            if (distanceToWaypoint < distanceTresh * 2 || vehicleStatus == Status.SLOW_DOWN) 
            {
                Vector3 futureVel = futureTargetTransform.position - targetTransform.position;
                futureSteering = Mathf.Clamp(this.transform.InverseTransformDirection(futureVel.normalized).x, -1, 1);
            }
            else
            {
                futureSteering = 0.0f;
            }

                //Check if the car has to stop
                if (vehicleStatus == Status.STOP){
                acc = 0;
                brake = 1;
                wheelDrive.maxSpeed = 0f;
            }
            else{
                
                //Not full acceleration if have to slow down
                if(vehicleStatus == Status.SLOW_DOWN){
                    acc = .3f;
                    brake = 0f;
                }

                //If planned to steer, decrease the speed
                if(futureSteering > .3f || futureSteering < -.3f){
                    wheelDrive.maxSpeed = Mathf.Min(wheelDrive.maxSpeed, wheelDrive.steeringSpeedMax);
                }
                else
                {
                    // Restore to original max speed if no sharp turn is planned
                    wheelDrive.maxSpeed = initMaxSpeed;
                }

                //2. Check if there are obstacles which are detected by the radar
                float hitDist;
                GameObject obstacle = GetDetectedObstacles(out hitDist);

                //Check if we hit something
                if(obstacle != null){

                    int layer = obstacle.gameObject.layer;

                    WheelDrive otherVehicle = null;
                    otherVehicle = obstacle.GetComponent<WheelDrive>();

                    VehicleAI otherVehicleAI = null;
                    otherVehicleAI = obstacle.GetComponent<VehicleAI>();

                    ///////////////////////////////////////////////////////////////
                    //Differenciate between other vehicles AI and generic obstacles (including controlled vehicle, if any)
                    if(otherVehicle != null){
                        //Check if it's front vehicle
                        float dotFront = Vector3.Dot(this.transform.forward, otherVehicle.transform.forward);

                        int otherTurn = 0;
                        int thisTurn = 0;

                        if (otherVehicleAI.futureSteering > 0.3f) otherTurn = 0;
                        else if (otherVehicleAI.futureSteering < -0.3f) otherTurn = 1;
                        else otherTurn = 2;

                        if (this.GetComponent<VehicleAI>().futureSteering > 0.3f) thisTurn = 0;
                        else if (this.GetComponent<VehicleAI>().futureSteering < -0.3f) thisTurn = 1;
                        else thisTurn = 2;

                        //If detected front vehicle max speed is lower than ego vehicle, then decrease ego vehicle max speed
                        if (otherVehicle.maxSpeed < wheelDrive.maxSpeed && dotFront > .8f){
                            //float ms = Mathf.Max(wheelDrive.GetSpeedMS(otherVehicle.maxSpeed) - .5f, .1f);
                            //wheelDrive.maxSpeed = wheelDrive.GetSpeedUnit(ms);
                            wheelDrive.maxSpeed = otherVehicle.maxSpeed * 0.8f;
                        }
                        
                        //If the two vehicles are too close, and facing the same direction, brake the ego vehicle
                        if(hitDist < emergencyBrakeThresh && dotFront > .8f){
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        //If the two vehicles are too close, and not facing same direction, slight make the ego vehicle go backward
                        else if(hitDist < emergencyBrakeThresh && dotFront <= .8f){
                            acc = -.3f;
                            brake = 0f;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);

                            //Check if the vehicle we are close to is located on the right or left then apply according steering to try to make it move
                            float dotRight = Vector3.Dot(this.transform.forward, otherVehicle.transform.right);
                            //Right
                            if(dotRight > 0.1f) steering = .3f;
                            //Left
                            else if(dotRight < -0.1f) steering = -.3f;
                            //Middle
                            else steering = -.7f;
                        }
                        //If the two vehicles are getting close, slow down their speed
                        else if(hitDist < slowDownThresh){
                            acc = .5f;
                            brake = 0f;
                            //wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 1.5f, wheelDrive.minSpeed);
                        }
                        if (otherVehicleAI.vehicleStatus == Status.SLOW_DOWN && this.GetComponent<VehicleAI>().vehicleStatus == Status.SLOW_DOWN)
                        {
                            //Debug.Log("Other vehicle is turning: " + otherTurn + "And this is turning: " + thisTurn);
                            // Check for priority based on turn direction
                            if (otherTurn == 1 && thisTurn != 1)
                            { // If the other vehicle is turning right, priority over any other turn
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {
                                    //wheelDrive.maxSpeed = otherVehicle.maxSpeed * 1.2f;
                                }
                            }
                            else if (otherTurn == 2 && thisTurn == 0)
                            { // If the other vehicle is going straight and we are turning left
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {
                                    //wheelDrive.maxSpeed = otherVehicle.maxSpeed * 1.2f;
                                }
                            }
                            else if (otherTurn == 0 && thisTurn != 0)
                            { // If the other vehicle is turning left, it never has priority
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {
                                    //wheelDrive.maxSpeed = otherVehicle.maxSpeed * 1.5f;
                                }
                            }
                            else if (otherTurn == 2 && thisTurn == 2)
                            { // If both are going straight, no priority change
                            }
                        }
                    }
                    else if (layer == LayerMask.NameToLayer("Player"))
                    {
                        float dotFront = Vector3.Dot(this.transform.forward, obstacle.transform.forward);

                        int thisTurn = 0;
                        if (this.GetComponent<VehicleAI>().futureSteering > 0.3f) thisTurn = 0;
                        else if (this.GetComponent<VehicleAI>().futureSteering < -0.3f) thisTurn = 1;
                        else thisTurn = 2;

                        string otherTurn = "";
                        otherTurn = vm._declaredDirection;

                        if (vm.GetMaxSpeed() * 5f < wheelDrive.maxSpeed && dotFront > .8f)
                        {
                            float ms = Mathf.Max(wheelDrive.GetSpeedMS(vm.GetMaxSpeed()) - .5f, .1f);
                            wheelDrive.maxSpeed = wheelDrive.GetSpeedUnit(ms);
                        }

                        if (hitDist < emergencyBrakeThresh && dotFront > .8f)
                        {
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        else if (hitDist < emergencyBrakeThresh && dotFront <= .8f)
                        {
                            acc = -.3f;
                            brake = 0f;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }
                        else if (hitDist < slowDownThresh)
                        {
                            acc = .5f;
                            brake = 0f;
                        }

                        if (this.GetComponent<VehicleAI>().vehicleStatus == Status.SLOW_DOWN)
                        {
                            if (otherTurn == "right" && thisTurn != 0)
                            {
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                    
                                }
                                else
                                {
                                }
                            }
                            else if (otherTurn == "straight" && thisTurn == 1)
                            {
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {

                                }
                            }
                            else if (otherTurn == "straight" && thisTurn == 0)
                            {
                                if (timeSinceLastCheck >= 5f)
                                {
                                    pm.AddIncident();
                                    Debug.Log("Wymuszenie pierwszeñstwa.");
                                    timeSinceLastCheck = 0f;
                                }
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {

                                }
                            }
                            else if (otherTurn == "left" && thisTurn != 1)
                            {
                                timeSinceLastCheck += Time.deltaTime;
                                if (timeSinceLastCheck >= 5f)
                                {
                                    pm.AddIncident();
                                    Debug.Log("Wymuszenie pierwszeñstwa.");
                                    timeSinceLastCheck = 0f;
                                }
                                if (hitDist < intersectionBrake)
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    ///////////////////////////////////////////////////////////////////
                    // Generic obstacles
                    else
                            {
                        //Emergency brake if getting too close
                        if(hitDist < emergencyBrakeThresh){
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        //Otherwise if getting relatively close decrease speed
                         else if(hitDist < slowDownThresh){
                            acc = .5f;
                            brake = 0f;
                        }
                    }
                }

                //Check if we need to steer to follow path
                if(acc > 0f){
                    Vector3 desiredVel = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform.position - this.transform.position;
                    steering = Mathf.Clamp(this.transform.InverseTransformDirection(desiredVel.normalized).x, -1f, 1f);
                }

            }

            wheelDrive.Move(acc, steering, brake, vehicleStatus);
        }


        GameObject GetDetectedObstacles(out float _hitDist){
            GameObject detectedObstacle = null;
            float minDist = 1000f;

            float speedFactor = Mathf.Clamp01(wheelDrive.maxSpeed * 0.5f / initMaxSpeed);
            raySpacing = Mathf.RoundToInt(Mathf.Lerp(2f, 8f, 1 - speedFactor));

            float initRay = (raysNumber / 2f) * raySpacing;
            float hitDist =  -1f;
            for(float a=-initRay; a<=initRay; a+=raySpacing){
                CastRay(raycastAnchor.transform.position, a, this.transform.forward, raycastLength, out detectedObstacle, out hitDist);

                if(detectedObstacle == null) continue;

                float dist = Vector3.Distance(this.transform.position, detectedObstacle.transform.position);
                if(dist < minDist) {
                    minDist = dist;
                    break;
                }
            }

            _hitDist = hitDist;
            return detectedObstacle;
        }

        
        void CastRay(Vector3 _anchor, float _angle, Vector3 _dir, float _length, out GameObject _outObstacle, out float _outHitDistance){
            _outObstacle = null;
            _outHitDistance = -1f;

            //Detect hit only on the autonomous vehicle layer
            int layer = 1 << LayerMask.NameToLayer("AutonomousVehicle");
            int playerLayer = 1 << LayerMask.NameToLayer("Player");
            int finalMask = layer | playerLayer;

            foreach(string layerName in trafficSystem.collisionLayers){
                int id = 1 << LayerMask.NameToLayer(layerName);
                finalMask = finalMask | id;
            }

            RaycastHit hit;
            if(Physics.Raycast(_anchor, Quaternion.Euler(0, _angle, 0) * _dir, out hit, _length, finalMask)){
                _outObstacle = hit.collider.gameObject;
                _outHitDistance = hit.distance;
                Debug.DrawRay(_anchor, Quaternion.Euler(0, _angle, 0) * _dir * _length, new Color(0, 1, 0, 0.5f));
            }
            else
            {
                //Draw raycast
                Debug.DrawRay(_anchor, Quaternion.Euler(0, _angle, 0) * _dir * _length, new Color(1, 0, 0, 0.5f));
            }
        }

        int GetNextSegmentId(){
            if(trafficSystem.segments[currentTarget.segment].nextSegments.Count == 0)
                return 0;
            int c = Random.Range(0, trafficSystem.segments[currentTarget.segment].nextSegments.Count);
            return trafficSystem.segments[currentTarget.segment].nextSegments[c].id;
        }

        void SetWaypointVehicleIsOn(){
            //Find current target
            foreach(Segment segment in trafficSystem.segments){
                if(segment.IsOnSegment(this.transform.position)){
                    currentTarget.segment = segment.id;

                    //Find nearest waypoint to start within the segment
                    float minDist = float.MaxValue;
                    for(int j=0; j<trafficSystem.segments[currentTarget.segment].waypoints.Count; j++){
                        float d = Vector3.Distance(this.transform.position, trafficSystem.segments[currentTarget.segment].waypoints[j].transform.position);

                        //Only take in front points
                        Vector3 lSpace = this.transform.InverseTransformPoint(trafficSystem.segments[currentTarget.segment].waypoints[j].transform.position);
                        if(d < minDist && lSpace.z > 0){
                            minDist = d;
                            currentTarget.waypoint = j;
                        }
                    }
                    break;
                }
            }

            //Get future target
            futureTarget.waypoint = currentTarget.waypoint + 1;
            futureTarget.segment = currentTarget.segment;

            if(futureTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                futureTarget.waypoint = 0;
                futureTarget.segment = GetNextSegmentId();
            }
        }

        public int GetSegmentVehicleIsIn(){
            int vehicleSegment = currentTarget.segment;
            bool isOnSegment = trafficSystem.segments[vehicleSegment].IsOnSegment(this.transform.position);
            if(!isOnSegment){
                bool isOnPSegement = trafficSystem.segments[pastTargetSegment].IsOnSegment(this.transform.position);
                if(isOnPSegement)
                    vehicleSegment = pastTargetSegment;
            }
            return vehicleSegment;
        }

        void HandleBlinkers()
        {
            // Left Arrow blinker
            if (futureSteering < -0.6f && !isLeftBlinkerOn)
            {
                if (leftBlinkerCoroutine != null) StopCoroutine(leftBlinkerCoroutine); // Stop previous coroutine if running
                isLeftBlinkerOn = true;
                leftBlinkerCoroutine = StartCoroutine(BlinkerCoroutine(leftBlinker));
            }

            else if (futureSteering > 0.6f && !isRightBlinkerOn)
            {
                if (rightBlinkerCoroutine != null) StopCoroutine(rightBlinkerCoroutine); // Stop previous coroutine if running
                isRightBlinkerOn = true;
                rightBlinkerCoroutine = StartCoroutine(BlinkerCoroutine(rightBlinker));

            }
            
            else if(futureSteering > -0.6f && futureSteering < 0.6f)
            {
                isLeftBlinkerOn = false;
                isRightBlinkerOn = false;
                if (rightBlinkerCoroutine != null)
                {
                    StopCoroutine(rightBlinkerCoroutine);
                    SetBlinker(rightBlinker, false);
                }
                if (leftBlinkerCoroutine != null)
                {
                    StopCoroutine(leftBlinkerCoroutine);
                    SetBlinker(leftBlinker, false);
                }
            }
        }
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
    }
}