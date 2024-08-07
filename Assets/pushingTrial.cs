using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushingTrial : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private bool trialActive = false;
    private float trialStartTime;
    private List<float> trialTimes = new List<float>();
    private int trialCount = 0;
    public int totalTrials = 4;
    public List<Transform> checkpoints; // List of checkpoint transforms
    private int currentCheckpointIndex = 0;

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
    }

    void StartTrial()
    {
        if (trialCount < totalTrials)
        {
            trialActive = true;
            trialStartTime = Time.time;
            currentCheckpointIndex = 0;
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
        RespawnCheckpoints();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (trialActive && other.CompareTag("Checkpoint"))
        {
            if (currentCheckpointIndex < checkpoints.Count && checkpoints[currentCheckpointIndex] == other.transform)
            {
                currentCheckpointIndex++;
                Debug.Log("Checkpoint " + currentCheckpointIndex + " reached.");
                Destroy(other.gameObject); // Remove the checkpoint

                if (currentCheckpointIndex >= checkpoints.Count)
                {
                    CompleteTrial();
                }
            }
        }
    }
    void RespawnCheckpoints()
    {
        foreach (Transform checkpoint in checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }
}
