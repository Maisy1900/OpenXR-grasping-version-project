using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    Vector3 initialPosition;
    Quaternion initialRotation;

    // Reference to the MainExperimentsetup script
    private MainExperimentsetup mainExperimentSetup;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Find the MainExperimentsetup script in the scene
        mainExperimentSetup = FindObjectOfType<MainExperimentsetup>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
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

        if (collision.gameObject.tag == "idx_tip")
        {
            // Notify the MainExperimentsetup script that this cube was touched
            //cube collides with an object tagged as "idx_tip" (presumably the fingertip or tool touching the cube), it triggers the CubeTouched method on the MainExperimentsetup script.
            if (mainExperimentSetup != null)
            {
                mainExperimentSetup.CubeTouched(gameObject);
            }
        }
    }

    public void ResetCubes()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
