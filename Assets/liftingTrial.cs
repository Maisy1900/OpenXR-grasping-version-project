using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class liftingTrial : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private bool trialActive = false;
    private float trialStartTime;
    private bool trialCompleted = false;
    public float targetHeight = 0.6f; // 60 cm

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (trialActive)
        {
            if (transform.position.y >= initialPosition.y + targetHeight)
            {
                CompleteTrial();
            }
        }
    }

    public void RunTrial()
    {
        if (!trialCompleted)
        {
            trialActive = true;
            trialStartTime = Time.time;
            Debug.Log("Trial started.");

            // Wait for the trial to complete
            StartCoroutine(WaitForCompletion());
        }
        else
        {
            Debug.Log("Trial already completed.");
        }
    }

    private IEnumerator WaitForCompletion()
    {
        while (trialActive)
        {
            yield return null; // Wait until the next frame
        }

        float trialTime = Time.time - trialStartTime;
        Debug.Log("Trial completed in " + trialTime + " seconds.");

        // Reset position, rotation, and velocities
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void CompleteTrial()
    {
        trialActive = false;
        trialCompleted = true; // Mark trial as completed
    }
}
