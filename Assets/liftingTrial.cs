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
    private List<float> trialTimes = new List<float>();
    private int trialCount = 0;
    public int totalTrials = 4;
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
        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartTrial();
        }

        if (trialActive)
        {
            if (transform.position.y >= initialPosition.y + targetHeight)
            {
                CompleteTrial();
            }
        }
    }

    void StartTrial()
    {
        if (trialCount < totalTrials)
        {
            trialActive = true;
            trialStartTime = Time.time;
            Debug.Log("Trial " + (trialCount + 1) + " started.");
        }
        else
        {
            Debug.Log("All trials completed.");
        }
    }

    void CompleteTrial()
    {
        trialActive = false;
        float trialTime = Time.time - trialStartTime;
        trialTimes.Add(trialTime);
        trialCount++;
        Debug.Log("Trial " + trialCount + " completed in " + trialTime + " seconds.");

        // Reset position, rotation, and velocities
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (trialCount >= totalTrials)
        {
            Debug.Log("All trials completed. Times: " + string.Join(", ", trialTimes));
        }
    }
}
