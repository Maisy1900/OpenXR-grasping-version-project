using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        if(collision.gameObject.tag == "idx_tip")
        {
            
        }
    }

    public void ResetCubes()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }


    // Add code to detect contact which cube is being touched to tell the MainExperimentsetup 
    


}
