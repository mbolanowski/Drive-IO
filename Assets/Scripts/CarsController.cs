using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarsController : MonoBehaviour
{
    public bool hasPriority = false;
    public bool leftBlinker = false;
    public bool rightBlinker = false;
    public bool isHorizontal = false;
    public string turning = "";
    public float speed;

    public int intersectionEntranceDirection = 5;
}
