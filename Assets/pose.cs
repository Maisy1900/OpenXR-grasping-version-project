using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;

public class pose : MonoBehaviour
{

    public Transform target;
    public Vector3 offset; 

    private void Update()
    {
        transform.position = target.position + offset; 
    }


}
