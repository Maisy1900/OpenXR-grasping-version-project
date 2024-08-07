using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPosition : MonoBehaviour
{

    Vector3 initialPosition;
    Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }    
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }


}
