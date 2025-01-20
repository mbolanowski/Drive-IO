using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public Transform raycastAnchor;

    [Header("Player Radar")]
    [Tooltip("Length of the casted rays for player detection")]
    public float raycastLengthPlayer = 8;
    [Tooltip("Spacing between each rays for player detection")]
    public int raySpacingPlayer = 3;
    [Tooltip("Number of rays to be casted for player detection")]
    public int raysNumberPlayer = 4;


    private float timeSinceLastCheck = 0f;

    public VechicleManager vm;
    public PlayerManager pm;
    public WarningSystemController wsc;
    private void Start()
    {
        if (vm == null) {
            vm = GameObject.Find("VechicleManager").GetComponent<VechicleManager>();
        }

        if (pm == null)
        {
            pm = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        }

        if (wsc == null)
        {
            wsc = GameObject.Find("Warning System").GetComponent<WarningSystemController>();
        }

    }
    private void Update()
    {
        float hitDist;
        GameObject obstacle = GetDetectedObstacles(out hitDist);


        if (obstacle != null)
        {
            float dotFront = Vector3.Dot(this.transform.forward, obstacle.transform.forward);

            CarsController cc = this.GetComponent<CarsController>();
            VehicleControllerWithGears vsc = obstacle.GetComponent<VehicleControllerWithGears>();

            string thisTurn = cc.turning;
            string otherTurn = vm._declaredDirection;

            bool _isHorizontal = cc.isHorizontal;
            bool _isPlayerHorizontal = vsc._isHorizontal;

            Vector3 directionToOther = this.transform.position - vsc.transform.position;
            float dotProduct = Vector3.Dot(vsc.transform.right, directionToOther.normalized);
            float value = 1.0f;

            if (cc.intersectionEntranceDirection != vsc.intersectionEntranceDirection)
            {
                if (thisTurn == "right" && otherTurn == "right")
                {

                }
                else if (thisTurn == "right" && otherTurn == "straight")
                {
                    if (_isHorizontal == _isPlayerHorizontal)
                    {

                    }
                    else
                    {
                        if (vsc.currentSpeed > 0.0f)
                        {
                            if (dotProduct > -value && dotProduct < value)
                            {
                                timeSinceLastCheck += Time.deltaTime;
                                if (timeSinceLastCheck >= 5f)
                                {
                                    if (!pm._hasRightOfWay && cc.hasPriority)
                                    {
                                        pm.AddIncident();
                                        pm.AddIncident();
                                        wsc.SetInfoText("Wymusiles pierwszenstwo");
                                        wsc.SetPenaltyText("-2 Life");
                                        Debug.Log("Wymuszenie pierwszeñstwa.1" + this.name);
                                        timeSinceLastCheck = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "right" && otherTurn == "left")
                {
                    if (_isPlayerHorizontal == _isHorizontal)
                    {
                        if (vsc.currentSpeed > 0.0f)
                        {
                            if (dotProduct > -value && dotProduct < value)
                            {
                                timeSinceLastCheck += Time.deltaTime;
                                if (timeSinceLastCheck >= 5f)
                                {
                                    if ((pm._hasRightOfWay == cc.hasPriority) || (!pm._hasRightOfWay && cc.hasPriority))
                                    {
                                        if (dotProduct > 0.3f)
                                        {
                                            pm.AddIncident();
                                            pm.AddIncident();
                                            wsc.SetInfoText("Wymusiles pierwszenstwo");
                                            wsc.SetPenaltyText("-2 Life");
                                            Debug.Log("Wymuszenie pierwszeñstwa.2 " + this.name);

                                            timeSinceLastCheck = 0f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "left" && otherTurn == "right")
                {
                    if (_isPlayerHorizontal == _isHorizontal)
                    {
                        if (vsc.currentSpeed > 0.0f)
                        {
                            if (dotProduct > -value && dotProduct < value)
                            {
                                if (timeSinceLastCheck >= 5f)
                                {
                                    if (!pm._hasRightOfWay && cc.hasPriority)
                                    {
                                        pm.AddIncident();
                                        pm.AddIncident();
                                        wsc.SetInfoText("Wymusiles pierwszenstwo");
                                        wsc.SetPenaltyText("-2 Life");
                                        Debug.Log("Wymuszenie pierwszeñstwa.3" + this.name);
                                        timeSinceLastCheck = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "left" && otherTurn == "straight")
                {
                    if (vsc.currentSpeed > 0.0f)
                    {
                        //Debug.Log("giga test222");
                        if (dotProduct > -value && dotProduct < value)
                        {
                            //Debug.Log("giga test");
                            if (timeSinceLastCheck >= 5f)
                            {
                                if (!pm._hasRightOfWay && cc.hasPriority)
                                {
                                    pm.AddIncident();
                                    pm.AddIncident();
                                    wsc.SetInfoText("Wymusiles pierwszenstwo");
                                    wsc.SetPenaltyText("-2 Life");
                                    Debug.Log("Wymuszenie pierwszeñstwa.4" + this.name);
                                    timeSinceLastCheck = 0f;
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "left" && otherTurn == "left")
                {
                    if (vsc.currentSpeed > 0.0f)
                    {
                        if (dotProduct > -value && dotProduct < value)
                        {
                            if (timeSinceLastCheck >= 5f)
                            {
                                if (!pm._hasRightOfWay && cc.hasPriority)
                                {
                                    pm.AddIncident();
                                    pm.AddIncident();
                                    wsc.SetInfoText("Wymusiles pierwszenstwo");
                                    wsc.SetPenaltyText("-2 Life");
                                    Debug.Log("Wymuszenie pierwszeñstwa.5" + this.name);
                                    timeSinceLastCheck = 0f;
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "straight" && otherTurn == "right")
                {
                    if (_isPlayerHorizontal == _isHorizontal)
                    {
                    }
                    else // if on the right yield, if on the left its okay
                    {
                        if (dotProduct < 0)
                        {
                            if (vsc.currentSpeed > 0.0f)
                            {
                                if (timeSinceLastCheck >= 5f)
                                {
                                    if (!pm._hasRightOfWay && cc.hasPriority)
                                    {
                                        pm.AddIncident();
                                        pm.AddIncident();
                                        wsc.SetInfoText("Wymusiles pierwszenstwo");
                                        wsc.SetPenaltyText("-2 Life");
                                        Debug.Log("Wymuszenie pierwszeñstwa.6" + this.name);
                                        timeSinceLastCheck = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "straight" && otherTurn == "straight")
                {
                    if (_isPlayerHorizontal == _isHorizontal)
                    {

                    }
                    else // if on the right yield, if on the left its okay
                    {
                        if (vsc.currentSpeed > 0.0f)
                        {
                            if (dotProduct > -value && dotProduct < value)
                            {
                                if (timeSinceLastCheck >= 5f)
                                {
                                    if (!pm._hasRightOfWay && cc.hasPriority)
                                    {
                                        pm.AddIncident();
                                        pm.AddIncident();
                                        wsc.SetInfoText("Wymusiles pierwszenstwo");
                                        wsc.SetPenaltyText("-2 Life");
                                        Debug.Log("Wymuszenie pierwszeñstwa.7" + this.name);
                                        timeSinceLastCheck = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (thisTurn == "straight" && otherTurn == "left")
                {
                    if (vsc.currentSpeed > 0.0f)
                    {
                        //Debug.Log("oh yea its real");
                        if (dotProduct > -value && dotProduct < value)
                        {
                            timeSinceLastCheck += Time.deltaTime;
                            if (timeSinceLastCheck >= 5f)
                            {
                                if ((pm._hasRightOfWay == cc.hasPriority) || (!pm._hasRightOfWay && cc.hasPriority))
                                {
                                    pm.AddIncident();
                                    pm.AddIncident();
                                    wsc.SetInfoText("Wymusiles pierwszenstwo");
                                    wsc.SetPenaltyText("-2 Life");
                                    Debug.Log("Wymuszenie pierwszeñstwa.8" + this.name);
                                    timeSinceLastCheck = 0f;
                                }
                            }
                        }
                    }
                }
            }

        }
    }

    GameObject GetDetectedObstacles(out float _hitDist)
    {
        GameObject detectedObstacle = null;
        float minDist = 1000f;
        float hitDist = -1f;

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

        if (playerObstacle != null)
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
}
