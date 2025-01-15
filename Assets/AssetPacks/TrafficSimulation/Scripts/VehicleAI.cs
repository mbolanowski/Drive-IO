// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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

        [Header("AI Vehicle Radar")]
        [Tooltip("Empty gameobject from where the rays will be casted")]
        public Transform raycastAnchor;
        [Tooltip("Length of the casted rays for AI vehicles")]
        public float raycastLengthAI = 5;
        [Tooltip("Spacing between each rays for AI vehicles")]
        public int raySpacingAI = 2;
        [Tooltip("Number of rays to be casted for AI vehicles")]
        public int raysNumberAI = 6;

        [Header("Player Radar")]
        [Tooltip("Length of the casted rays for player detection")]
        public float raycastLengthPlayer = 8;
        [Tooltip("Spacing between each rays for player detection")]
        public int raySpacingPlayer = 3;
        [Tooltip("Number of rays to be casted for player detection")]
        public int raysNumberPlayer = 4;

        [Tooltip("If detected vehicle is below this distance, ego vehicle will stop")]
        public float emergencyBrakeThresh = 2f;

        [Tooltip("If detected vehicle is below this distance at intersection, the vehicle will stop")]
        public float intersectionBrake = 2f;

        [Tooltip("If detected vehicle is below this distance (and above, above distance), ego vehicle will slow down")]
        public float slowDownThresh = 4f;

        private float timeSinceLastCheck = 0f;

        public int intersectionEntranceDirection = 5; //0=UP, 1=RIGHT, 2=DOWN, 3=LEFT

        [SerializeField] public Status vehicleStatus = Status.GO;

        private WheelDrive wheelDrive;
        private CarsManager cm;
        private VechicleManager vm;
        private PlayerManager pm;
        private WarningSystemController wsc;
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

        public int placeInQueue = 5;

        public float brake = 0;

        public float savedSteering = 0.0f;

        public bool _isHorizontal;

        public bool _TurningLeft = false;
        public bool _TurningRight = false;
        public bool _TurningStraight = false;

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

            if (wsc == null)
            {
                wsc = GameObject.Find("Warning System").GetComponent<WarningSystemController>();
            }

            initMaxSpeed = wheelDrive.maxSpeed;
            SetWaypointVehicleIsOn();

            intersectionEntranceDirection = GetSegmentObject().intersectionEntranceDirection;
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
            brake = 0;
            float steering = 0;
            wheelDrive.maxSpeed = initMaxSpeed;

            //Calculate if there is a planned turn
            Transform targetTransform = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform;
            Transform futureTargetTransform = trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].transform;
            //Debug.Log(trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].name);

            // Distance to the current waypoint
            float distanceToWaypoint = Vector3.Distance(this.transform.position, targetTransform.position);

            if (distanceToWaypoint < distanceTresh * 10 || vehicleStatus == Status.SLOW_DOWN) 
            {
                //Debug.Log("Distance: " + distanceToWaypoint + " to waypoint: " + targetTransform.gameObject.name);
                Vector3 futureVel = futureTargetTransform.position - targetTransform.position;
                futureSteering = Mathf.Clamp(this.transform.InverseTransformDirection(futureVel.normalized).x, -1, 1);

                foreach (Segment segment in trafficSystem.segments)
                {
                    if (segment.IsOnSegment(this.transform.position))
                    {
                        _isHorizontal = segment._trueIfHorizontal;
                    }
                }
            }
            else
            {
                //futureSteering *= 0.1f;
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

                if (vehicleStatus == Status.GO)
                {
                    savedSteering = futureSteering;
                }

                //If planned to steer, decrease the speed
                if((savedSteering < .3f && savedSteering > -.3f) && vehicleStatus == Status.SLOW_DOWN)
                {
                    wheelDrive.maxSpeed = initMaxSpeed;
                    //Debug.Log(futureSteering);
                }
                else if(vehicleStatus == Status.SLOW_DOWN)
                {
                    wheelDrive.maxSpeed = Mathf.Min(wheelDrive.maxSpeed, wheelDrive.steeringSpeedMax);
                    //Debug.Log("SLOW DOWN DAMN " + wheelDrive.maxSpeed);
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
                            //wheelDrive.maxSpeed = otherVehicle.maxSpeed * 0.8f;
                        }
                        
                        //If the two vehicles are too close, and facing the same direction, brake the ego vehicle
                        if(hitDist < emergencyBrakeThresh && dotFront > .5f){
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        //If the two vehicles are too close, and not facing same direction, slight make the ego vehicle go backward
                        else if(hitDist < (emergencyBrakeThresh + 0.4f) && dotFront <= .8f){
                            //acc = 0;
                            //brake = 1;
                            //wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);

                            //Check if the vehicle we are close to is located on the right or left then apply according steering to try to make it move
                            //float dotRight = Vector3.Dot(this.transform.forward, otherVehicle.transform.right);
                            //Right
                            //if(dotRight > 0.3f) steering = -.2f;
                            //Left
                            //else if(dotRight < -0.3f) steering = .2f;
                            //Middle
                            //else steering = -.7f;
                        }
                        //If the two vehicles are getting close, slow down their speed
                        else if(hitDist < slowDownThresh){
                            acc = .5f;
                            brake = 0f;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 1.5f, wheelDrive.minSpeed);
                        }
                        if (otherVehicleAI.vehicleStatus == Status.SLOW_DOWN && this.GetComponent<VehicleAI>().vehicleStatus == Status.SLOW_DOWN)
                        {
                            //Debug.Log("Other vehicle is turning: " + otherTurn + "And this is turning: " + thisTurn);
                            // Check for priority based on turn direction                                                           2 STRAIGHT, 0 RIGHT, 1 LEFT
                            bool thisHorizontal = this.GetComponent<VehicleAI>()._isHorizontal;
                            bool otherHorizontal = otherVehicleAI._isHorizontal;
                            if (thisTurn == 1 && otherTurn == 0)
                            {
                                if ((thisHorizontal && otherHorizontal) || (!thisHorizontal && !otherHorizontal))
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                            }
                            else if (thisTurn == 1 && otherTurn == 1)
                            {
                                if(placeInQueue > otherVehicleAI.placeInQueue && ((thisHorizontal && otherHorizontal)) || ((!thisHorizontal && !otherHorizontal)))
                                {
                                    acc = 0;
                                    brake = 1;
                                    float dotRight = Vector3.Dot(this.transform.forward, otherVehicle.transform.right);
                                    //Right
                                    if (dotRight > 0.3f) steering = .4f;
                                    //Left
                                    else if (dotRight < -0.3f) steering = -.4f;
                                    //Middle
                                    else steering = -.7f;
                                }
                                else
                                {
                                }
                            }
                            else if (thisTurn == 1 && otherTurn == 2)
                            {
                                acc = 0;
                                brake = 1;
                            }

                            else if (thisTurn == 2 && otherTurn == 0)
                            {
                                if ((!(thisHorizontal && otherHorizontal)) || (!(thisHorizontal && !otherHorizontal)))
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                                else
                                {
                                }
                            }
                            else if (thisTurn == 2 && otherTurn == 1)
                            {
                            }
                            else if (thisTurn == 2 && otherTurn == 2)
                            {
                                if ((thisHorizontal && otherHorizontal) || (!thisHorizontal && !otherHorizontal))
                                {
                                }
                                else
                                {
                                    acc = 0;
                                    brake = 1;
                                }
                            }

                            else if (thisTurn == 0 && otherTurn == 0)
                            {
                            }
                            else if (thisTurn == 0 && otherTurn == 1)
                            {
                            }
                            else if (thisTurn == 0 && otherTurn == 2)
                            {
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


                        VehicleControllerWithGears vsc = obstacle.GetComponent<VehicleControllerWithGears>();
                        Vector3 directionToOther = transform.position - vsc.transform.position;
                        float dotProduct = Vector3.Dot(vsc.transform.right, directionToOther.normalized);
                        float value = 1.0f;

                        if (this.GetComponent<VehicleAI>().vehicleStatus == Status.SLOW_DOWN && (this.GetComponent<VehicleAI>().intersectionEntranceDirection != vsc.intersectionEntranceDirection))
                        {
                            if (thisTurn == 0 && otherTurn == "right")
                            {

                            }
                            else if (thisTurn == 0 && otherTurn == "straight")
                            {
                                if (vsc._isHorizontal == _isHorizontal)
                                {

                                }
                                else
                                {
                                    if (vsc.currentSpeed > 0.2f)
                                    {
                                        if (dotProduct > -value && dotProduct < value)
                                        {
                                            acc = 0;
                                            brake = 1;
                                        }
                                    }
                                }
                            }
                            else if (thisTurn == 0 && otherTurn == "left")
                            {
                                if (vsc._isHorizontal == _isHorizontal)
                                {
                                    if (vsc.currentSpeed > 0.2f)
                                    {
                                        if (dotProduct > -value && dotProduct < value)
                                        {
                                            acc = 0;
                                            brake = 1;
                                            timeSinceLastCheck += Time.deltaTime;
                                            if (timeSinceLastCheck >= 5f)
                                            {
                                                pm.AddIncident();
                                                pm.AddIncident();
                                                wsc.SetInfoText("Wymusiles pierwszenstwo");
                                                wsc.SetPenaltyText("-2 Life");
                                                Debug.Log("Wymuszenie pierwszeñstwa.");
                                                timeSinceLastCheck = 0f;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (thisTurn == 1 && otherTurn == "right")
                            {
                                if (vsc._isHorizontal == _isHorizontal)
                                {
                                    if (vsc.currentSpeed > 0.2f)
                                    {
                                        if (dotProduct > -value && dotProduct < value)
                                        {
                                            acc = 0;
                                            brake = 1;
                                        }
                                    }
                                }
                            }
                            else if (thisTurn == 1 && otherTurn == "straight")
                            {
                                if (vsc.currentSpeed > 0.2f)
                                {
                                    Debug.Log("giga test222");
                                    if (dotProduct > -value && dotProduct < value)
                                    {
                                        Debug.Log("giga test");
                                        acc = 0;
                                        brake = 1;
                                    }
                                }
                            }
                            else if (thisTurn == 1 && otherTurn == "left")
                            {
                                if (vsc.currentSpeed > 0.2f)
                                {
                                    if (dotProduct > -value && dotProduct < value)
                                    {
                                        acc = 0;
                                        brake = 1;
                                    }
                                }
                            }
                            else if (thisTurn == 2 && otherTurn == "right")
                            {
                                if (vsc._isHorizontal == _isHorizontal)
                                {

                                }
                                else // if on the right yield, if on the left its okay
                                {
                                    if (dotProduct > 0)
                                    {
                                        if (vsc.currentSpeed > 0.2f)
                                        {
                                            acc = 0;
                                            brake = 1;
                                        }
                                    }
                                }
                            }
                            else if (thisTurn == 2 && otherTurn == "straight")
                            {
                                if (vsc._isHorizontal == _isHorizontal)
                                {

                                }
                                else // if on the right yield, if on the left its okay
                                {
                                    if (vsc.currentSpeed > 0.2f)
                                    {
                                        if (dotProduct > -value && dotProduct < value)
                                        {
                                            acc = 0;
                                            brake = 1;
                                        }
                                    }
                                }
                            }
                            else if (thisTurn == 2 && otherTurn == "left")
                            {
                                if (vsc.currentSpeed > 0.2f)
                                {
                                    Debug.Log("oh yea its real");
                                    if (dotProduct > -value && dotProduct < value)
                                    {
                                        acc = 0;
                                        brake = 1;
                                        timeSinceLastCheck += Time.deltaTime;
                                        if (timeSinceLastCheck >= 5f)
                                        {
                                            pm.AddIncident();
                                            pm.AddIncident();
                                            wsc.SetInfoText("Wymusiles pierwszenstwo");
                                            wsc.SetPenaltyText("-2 Life");
                                            Debug.Log("Wymuszenie pierwszeñstwa.");
                                            timeSinceLastCheck = 0f;
                                        }
                                    }
                                }
                            }
                               
                                /*timeSinceLastCheck += Time.deltaTime;
                                if (timeSinceLastCheck >= 5f)
                                {
                                    pm.AddIncident();
                                    pm.AddIncident();
                                    wsc.SetInfoText("Wymusiles pierwszenstwo");
                                    wsc.SetPenaltyText("-2 Life");
                                    Debug.Log("Wymuszenie pierwszeñstwa.");
                                    timeSinceLastCheck = 0f;
                                }*/
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


        GameObject GetDetectedObstacles(out float _hitDist)
        {
            GameObject detectedObstacle = null;
            float minDist = 1000f;
            float hitDist = -1f;

            // First raycast for AI vehicles
            GameObject aiObstacle = CastRaysForLayer(
                raycastAnchor.transform.position,
                this.transform.forward,
                raycastLengthAI,
                raySpacingAI,
                raysNumberAI,
                LayerMask.NameToLayer("AutonomousVehicle"),
                out float aiHitDist
            );

            // Second raycast for Player
            GameObject playerObstacle = CastRaysForLayer(
                raycastAnchor.transform.position,
                this.transform.forward,
                raycastLengthPlayer,
                raySpacingPlayer,
                raysNumberPlayer,
                LayerMask.NameToLayer("Player"),
                out float playerHitDist
            );

            GameObject CastRaysForLayer(Vector3 anchor, Vector3 direction, float rayLength, int raySpacing, int raysNumber, int layer, out float outHitDist)
            {
                GameObject detectedObstacle = null;
                float minDist = 1000f;
                outHitDist = -1f;

                float initRay = (raysNumber / 2f) * raySpacing;

                // Create layer mask for specific layer
                int layerMask = 1 << layer;

                // Add additional collision layers if needed (only for AI vehicles)
                if (layer == LayerMask.NameToLayer("AutonomousVehicle"))
                {
                    foreach (string layerName in trafficSystem.collisionLayers)
                    {
                        int id = 1 << LayerMask.NameToLayer(layerName);
                        layerMask = layerMask | id;
                    }
                }

                for (float a = -initRay; a <= initRay; a += raySpacing)
                {
                    GameObject obstacle;
                    float hitDist;
                    CastRay(anchor, a, direction, rayLength, layerMask, out obstacle, out hitDist);

                    if (obstacle == null) continue;

                    float dist = Vector3.Distance(this.transform.position, obstacle.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        detectedObstacle = obstacle;
                        outHitDist = hitDist;
                    }
                }

                return detectedObstacle;
            }

            // Determine which obstacle is closer (if any)
            if (aiObstacle != null && (playerObstacle == null || aiHitDist < playerHitDist))
            {
                detectedObstacle = aiObstacle;
                hitDist = aiHitDist;
            }
            else if (playerObstacle != null)
            {
                detectedObstacle = playerObstacle;
                hitDist = playerHitDist;
            }

            _hitDist = hitDist;
            return detectedObstacle;
        }


        void CastRay(Vector3 _anchor, float _angle, Vector3 _dir, float _length, int layerMask, out GameObject _outObstacle, out float _outHitDistance)
        {
            _outObstacle = null;
            _outHitDistance = -1f;

            RaycastHit hit;
            if (Physics.Raycast(_anchor, Quaternion.Euler(0, _angle, 0) * _dir, out hit, _length, layerMask))
            {
                _outObstacle = hit.collider.gameObject;
                _outHitDistance = hit.distance;
                Debug.DrawRay(_anchor, Quaternion.Euler(0, _angle, 0) * _dir * _length, new Color(0, 1, 0, 0.5f));
            }
            else
            {
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

        public Segment GetSegmentObject()
        {
            int vehicleSegment = currentTarget.segment;
            bool isOnSegment = trafficSystem.segments[vehicleSegment].IsOnSegment(this.transform.position);
            if (!isOnSegment)
            {
                bool isOnPSegement = trafficSystem.segments[pastTargetSegment].IsOnSegment(this.transform.position);
                if (isOnPSegement)
                    vehicleSegment = pastTargetSegment;
            }
            return trafficSystem.segments[vehicleSegment];
        }

        void HandleBlinkers()
        {

            //Calculate if there is a planned turn
            Transform targetTransform = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform;
            Transform futureTargetTransform = trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].transform;
            //Debug.Log(trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].name);

            // Distance to the current waypoint
            float distanceToWaypoint = Vector3.Distance(this.transform.position, targetTransform.position);

            if (distanceToWaypoint < distanceTresh * 5)
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
            }
            else
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