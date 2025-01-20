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
    public string actualID = string.Empty;

    private Coroutine leftBlinkerCoroutine;
    private Coroutine rightBlinkerCoroutine;
    public GameObject leftBlinkerObj;
    public GameObject rightBlinkerObj;

    public int intersectionEntranceDirection = 5;

    private void Update()
    {
        HandleBlinker(ref rightBlinkerCoroutine, rightBlinkerObj, rightBlinker);
        HandleBlinker(ref leftBlinkerCoroutine, leftBlinkerObj, leftBlinker);
    }

    private void HandleBlinker(ref Coroutine coroutine, GameObject blinkerObj, bool isActive)
    {
        if (isActive)
        {
            if (coroutine == null)
            {
                SetBlinker(blinkerObj, false); // Ensure initial state
                coroutine = StartCoroutine(BlinkerCoroutine(blinkerObj));
            }
        }
        else
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null; // Important: Reset the reference
                SetBlinker(blinkerObj, false);
            }
        }
    }

    private IEnumerator BlinkerCoroutine(GameObject blinker)
    {
        WaitForSeconds halfSecond = new WaitForSeconds(0.5f); // Cache for better performance

        while (true)
        {
            if (blinker == null) // Safety check
            {
                yield break;
            }

            SetBlinker(blinker, true);
            yield return halfSecond;
            SetBlinker(blinker, false);
            yield return halfSecond;
        }
    }

    private void SetBlinker(GameObject blinker, bool isActive)
    {
        if (blinker != null)
        {
            blinker.SetActive(isActive);
        }
    }

    // Clean up when the component is disabled or destroyed
    private void OnDisable()
    {
        if (leftBlinkerCoroutine != null)
        {
            StopCoroutine(leftBlinkerCoroutine);
            leftBlinkerCoroutine = null;
        }

        if (rightBlinkerCoroutine != null)
        {
            StopCoroutine(rightBlinkerCoroutine);
            rightBlinkerCoroutine = null;
        }

        // Ensure blinkers are off
        SetBlinker(leftBlinkerObj, false);
        SetBlinker(rightBlinkerObj, false);
    }
}
