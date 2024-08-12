using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*To run the trial call RunTrial(), currently update function looks for input:Y to start the trial. 
 * Time is recorded from the start to the end of this trial, 
 * the cube is respawned in its default position.
 */
public class liftingTrial : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public Rigidbody rb; 
    private bool trialActive = false;
    private float trialStartTime;
    private bool trialCompleted = false;
    public float targetHeight = 0.6f; // 60 cm
    public GraspHold graspHoldScript;

    // Start is called before the first frame update
    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        //graspHoldScript = GetComponent<GraspHold>();


        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Start the trial when "Y" key is pressed
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RunTrial();
        }

        if (trialActive)
        {
            if (transform.position.y >= initialPosition.y + targetHeight)
            {
                CompleteTrial();
            }
        }
    }

    void FixedUpdate()
    {
        // Avoid resetting velocity and angular velocity during FixedUpdate unless necessary
        // This can interfere with natural physics behavior like gravity
    }

    public void RunTrial()
    {
        // Allow trial to be restarted
        trialCompleted = false;

        // Reset grasp state
        if (graspHoldScript != null)
        {
            graspHoldScript.ResetGraspState();
        }

        // Reset position and rotation of the cube
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Temporarily disable the Rigidbody to fully reset it
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // After resetting position, ensure gravity is enabled and physics are active
        rb.isKinematic = false;
        rb.useGravity = true; // Ensure gravity is enabled after reset

        trialActive = true;
        trialStartTime = Time.time;
        Debug.Log("Trial started.");

        // Wait for the trial to complete
        StartCoroutine(WaitForCompletion());
    }

    private IEnumerator WaitForCompletion()
    {
        while (trialActive)
        {
            yield return null; // Wait until the next frame
        }

        float trialTime = Time.time - trialStartTime;
        Debug.Log("Trial completed in " + trialTime + " seconds.");

        // Reset grasp state at the end of the trial
        if (graspHoldScript != null)
        {
            graspHoldScript.ResetGraspState();
        }

        // Reset position, rotation, and velocities
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // After resetting position, ensure gravity is enabled and physics are active
        rb.isKinematic = false;
        rb.useGravity = true; // Ensure gravity is enabled after reset

        // Allow trial to be restarted
        trialCompleted = false;
    }

    void CompleteTrial()
    {
        trialActive = false;
        trialCompleted = true; // Mark trial as completed
    }
}
