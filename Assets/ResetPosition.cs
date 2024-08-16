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

    // Index of the cube, unique for each cube
    public int cubeIndex;

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
        // Reset the position when the cube hits the floor
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    if (collision.gameObject.tag == "idx_tip")
    {
        // Notify MainExperimentsetup that the cube was touched
        Debug.Log("Cube " + cubeIndex + " touched by idx_tip.");
        if (mainExperimentSetup != null)
        {
                int trialNumber = mainExperimentSetup.currentTrialNumber;

                // Call CubeTouched with the trial number
                bool firstTouch = mainExperimentSetup.CubeTouched(gameObject, cubeIndex, trialNumber);

        }
    }
}


    public void ResetCubes()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
