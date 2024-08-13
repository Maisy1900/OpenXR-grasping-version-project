using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* PushingTrial Script to use in external script use RunTrial() - if hand animation is over and not all checkpoints are hit then record hit and missed checkpoints and end trial (EndTrialEarly())
 * - Manages a pushing task with checkpoints.
 * - Tracks and resets the trial state, including checkpoints.
 * - Allows the trial to be restarted and tracked for completion.
 */

public class PushingTrial : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private bool trialActive = false;
    private float trialStartTime;
    private List<float> trialTimes = new List<float>();
    public List<Transform> checkpoints; // List of checkpoint transforms
    private int currentCheckpointIndex = 0;
    private int checkpointsHit = 0;
    private int checkpointsMissed = 0;
    private GraspHold graspHoldScript; // Reference to the GraspHold script

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        graspHoldScript = GetComponent<GraspHold>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Initially reset checkpoints
        RespawnCheckpoints();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RunTrial();
        }
    }

    public void RunTrial()
    {
        // Master function to start or reset the trial
        StartTrial();
    }

    void StartTrial()
    {
        trialActive = true;
        trialStartTime = Time.time;
        currentCheckpointIndex = 0;
        checkpointsHit = 0;
        checkpointsMissed = 0;
        RespawnCheckpoints();

        // Disable the grasping logic when the trial starts
        if (graspHoldScript != null)
        {
            graspHoldScript.enabled = false;
        }

        Debug.Log("Trial started.");
    }

    public void CompleteTrial()
    {
        trialActive = false;
        float trialTime = Time.time - trialStartTime;
        trialTimes.Add(trialTime);
        Debug.Log("Trial completed in " + trialTime + " seconds. Checkpoints hit: " + checkpointsHit + ", missed: " + checkpointsMissed);

        // Reset position, rotation, and velocities
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Re-enable the grasping logic after the trial is completed
        if (graspHoldScript != null)
        {
            graspHoldScript.enabled = true;
        }
    }
    //if the hand animation is over then do this function 
    public void EndTrialEarly()
    {
        // End the trial early if the external script signals it
        Debug.Log("External signal received. Finishing trial early.");
        for (int i = currentCheckpointIndex; i < checkpoints.Count; i++)
        {
            checkpointsMissed++;
            Debug.Log("Checkpoint " + (i + 1) + " missed.");
        }
        CompleteTrial();
    }

    public bool IsTrialActive()
    {
        return trialActive;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (trialActive && other.CompareTag("Checkpoint"))
        {
            if (currentCheckpointIndex < checkpoints.Count)
            {
                if (checkpoints[currentCheckpointIndex] == other.transform)
                {
                    // Correct checkpoint reached
                    checkpointsHit++;
                    currentCheckpointIndex++;
                    Debug.Log("Checkpoint " + currentCheckpointIndex + " reached.");
                    other.gameObject.SetActive(false); // Deactivate the checkpoint

                    if (currentCheckpointIndex >= checkpoints.Count)
                    {
                        CompleteTrial();
                    }
                }
                else
                {
                    // Miss the current checkpoint, but move on to the next
                    checkpointsMissed++;
                    Debug.Log("Checkpoint missed! Total missed: " + checkpointsMissed);
                    currentCheckpointIndex++; // Move to the next checkpoint

                    if (checkpoints[currentCheckpointIndex] == other.transform)
                    {
                        checkpointsHit++;
                        currentCheckpointIndex++;
                        Debug.Log("Checkpoint " + currentCheckpointIndex + " reached.");
                        other.gameObject.SetActive(false); // Deactivate the checkpoint

                        if (currentCheckpointIndex >= checkpoints.Count)
                        {
                            CompleteTrial();
                        }
                    }
                }
            }
        }
    }

    void RespawnCheckpoints()
    {
        // Reset all checkpoints to their initial state
        foreach (Transform checkpoint in checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
            // Optionally, you could reset checkpoint positions here if they move during the trial
        }
    }
}
