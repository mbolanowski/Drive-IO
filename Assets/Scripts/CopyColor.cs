using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class CopyColor : MonoBehaviour
{

    public VechicleManager vm;
    // Start is called before the first frame update
    void Start()
    {
        if (vm == null)
        {
            vm = GameObject.Find("VechicleManager").GetComponent<VechicleManager>();
        }

        GetComponent<Renderer>().material.color = vm.vehicleColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
