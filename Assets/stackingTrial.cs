using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// StackingTrial Script
/// 
/// How to Use:
/// - Attach this script to a GameObject in your Unity scene.
/// - Assign the baseCube, midCube, topCube, and planeGuideline references in the Unity Editor.
/// - Call the RunTrial() method from an external script (e.g., MainSimulator) to reset and start the stacking trial.
/// - Press the "Y" key during gameplay to manually start or restart the trial.
///
/// Success Conditions:
/// - The trial is considered successful when all three cubes are stacked correctly:
///   1. The baseCube is aligned with the planeGuideline.
///   2. The midCube is aligned with the baseCube in both position and rotation.
///   3. The topCube is aligned with the midCube in both position and rotation.
/// - Each cube must also be settled (i.e., have a velocity below the threshold) before being locked in place.
///
/// Ending the Trial Early:
/// - Call the EndTrialEarly() method from an external script to end the trial prematurely.
/// - This will record the current state and finish the trial, even if all cubes are not yet aligned.
/// 
/// Multiple Trials:
/// - The trial can be restarted multiple times by pressing the "Y" key or by calling RunTrial() externally.
/// - Each time the trial starts, the cubes are reset to their initial positions and states.
/// </summary>
public class StackingTrial : MonoBehaviour
{
    public Transform baseCube, midCube, topCube; // References to the three cubes
    public Transform planeGuideline; // Reference to the plane guideline

    private Vector3 baseInitialPosition, midInitialPosition, topInitialPosition;
    private Quaternion baseInitialRotation, midInitialRotation, topInitialRotation;
    private bool trialActive = false;
    private float trialStartTime;
    private List<string> stackingResults = new List<string>();
    private int currentStep = 0;
    private const float alignmentThreshold = 0.005f; // 5mm threshold for horizontal alignment
    private const float rotationThreshold = 10f; // 10 degrees threshold for rotation alignment
    private const float velocityThreshold = 0.001f; // to account for noise 

    private string path;

    // Flags to check if cubes are locked
    private bool baseCubeLocked = false;
    private bool midCubeLocked = false;
    private bool topCubeLocked = false;

    private void Start()
    {
        // Initialize positions and rotations
        baseInitialPosition = baseCube.position;
        baseInitialRotation = baseCube.rotation;
        midInitialPosition = midCube.position;
        midInitialRotation = midCube.rotation;
        topInitialPosition = topCube.position;
        topInitialRotation = topCube.rotation;

        // Initialize CSV path
        path = Application.dataPath + "/StackingResults";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Add CSV headers
        stackingResults.Add("Step,Cube,PositionX,PositionY,PositionZ,AlignmentWithPrevious,AlignmentWithGuideline,RotationAlignment,Timestamp");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RunTrial(); // Allow the trial to be restarted multiple times by pressing "Y"
        }

        if (trialActive)
        {
            CheckAlignment();
        }
    }

    public void RunTrial()
    {
        ResetCubes(); // Reset the cubes before starting the trial
        StartTrial();
    }

    private void StartTrial()
    {
        trialActive = true;
        trialStartTime = Time.time;
        currentStep = 1;
        Debug.Log("Trial started.");
    }

    private void CheckAlignment()
    {
        Transform currentCube = null;
        Transform previousCube = null;
        bool isCurrentCubeLocked = false;
        float alignmentWithPrevious = 0f;
        float rotationDifference = 0f;

        switch (currentStep)
        {
            case 1:
                currentCube = baseCube;
                previousCube = planeGuideline;
                isCurrentCubeLocked = baseCubeLocked;
                break;
            case 2:
                currentCube = midCube;
                previousCube = baseCube;
                isCurrentCubeLocked = midCubeLocked;
                alignmentWithPrevious = Vector3.Distance(new Vector3(currentCube.position.x, 0, currentCube.position.z), new Vector3(previousCube.position.x, 0, previousCube.position.z));
                rotationDifference = Quaternion.Angle(currentCube.rotation, previousCube.rotation);
                break;
            case 3:
                currentCube = topCube;
                previousCube = midCube;
                isCurrentCubeLocked = topCubeLocked;
                alignmentWithPrevious = Vector3.Distance(new Vector3(currentCube.position.x, 0, currentCube.position.z), new Vector3(previousCube.position.x, 0, previousCube.position.z));
                rotationDifference = Quaternion.Angle(currentCube.rotation, previousCube.rotation);
                break;
            default:
                CompleteTrial();
                return;
        }

        if (isCurrentCubeLocked)
        {
            Debug.Log($"{currentCube.name} is already locked.");
            return;
        }

        // Calculate position alignment ignoring Y direction
        float alignmentWithGuideline = Vector3.Distance(new Vector3(currentCube.position.x, 0, currentCube.position.z), new Vector3(planeGuideline.position.x, 0, planeGuideline.position.z));

        Rigidbody rb = currentCube.GetComponent<Rigidbody>();

        // Debugging information to understand the alignment and velocity
        Debug.Log($"{currentCube.name} - Alignment with Previous: {alignmentWithPrevious}, Alignment with Guideline: {alignmentWithGuideline}, Rotation Difference: {rotationDifference}, Velocity: {rb.velocity.magnitude}");

        // Check if the rotation difference is within acceptable ranges (near multiples of 90 degrees)
        bool isRotationAligned = (Mathf.Abs(rotationDifference % 90) <= rotationThreshold || Mathf.Abs((rotationDifference % 90) - 90) <= rotationThreshold);

        // Check if the cube is aligned correctly
        bool isAligned = (currentStep == 1 && alignmentWithGuideline <= alignmentThreshold) ||
                         (currentStep > 1 && alignmentWithPrevious <= alignmentThreshold && isRotationAligned);

        // Detailed debug logs
        if (currentStep == 1)
        {
            Debug.Log($"{currentCube.name} Alignment with Guideline: {alignmentWithGuideline} <= {alignmentThreshold}");
        }
        else
        {
            Debug.Log($"{currentCube.name} Alignment with Previous: {alignmentWithPrevious} <= {alignmentThreshold}");
            Debug.Log($"{currentCube.name} Rotation Difference: {rotationDifference} aligned: {isRotationAligned}");
        }

        // If the cube is aligned, has low velocity, and is not parented, lock it in place and change its color
        if (isAligned && rb.velocity.magnitude <= velocityThreshold && currentCube.parent == null)
        {
            Debug.Log($"{currentCube.name} is in alignment and settled.");
            ChangeCubeColor(currentCube, Color.green); // Change color to green when aligned
            RecordAlignment(currentCube, alignmentWithPrevious, alignmentWithGuideline, rotationDifference);
            LockCube(currentCube, previousCube);
            currentStep++;
            if (currentStep > 3)
            {
                CompleteTrial();
            }
        }
        else
        {
            if (rb.velocity.magnitude > velocityThreshold)
            {
                Debug.Log($"{currentCube.name} is not settled. Velocity: {rb.velocity.magnitude} > {velocityThreshold}");
            }
            if (currentCube.parent != null)
            {
                Debug.Log($"{currentCube.name} is still parented to {currentCube.parent.name}");
            }
            if (!isAligned)
            {
                Debug.Log($"{currentCube.name} is not aligned.");
            }
        }
    }

    private void ChangeCubeColor(Transform cube, Color color)
    {
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void RecordAlignment(Transform cube, float alignmentWithPrevious, float alignmentWithGuideline, float rotationDifference)
    {
        float timestamp = Time.time - trialStartTime;
        string result = $"{currentStep},{cube.name},{cube.position.x},{cube.position.y},{cube.position.z},{alignmentWithPrevious},{alignmentWithGuideline},{rotationDifference},{timestamp}";
        stackingResults.Add(result);
        Debug.Log($"Step {currentStep} completed for {cube.name}. Time: {timestamp} seconds, Alignment with previous: {alignmentWithPrevious}m, Alignment with guideline: {alignmentWithGuideline}m, Rotation difference: {rotationDifference} degrees");
    }

    private void LockCube(Transform cube, Transform previousCube)
    {
        Rigidbody rb = cube.GetComponent<Rigidbody>();

        // Check if the cube's Y velocity is close to zero, indicating that it has settled
        if (Mathf.Abs(rb.velocity.y) > velocityThreshold) // Use a small threshold instead of zero
        {
            Debug.Log($"{cube.name} is still moving vertically. Y-Velocity: {rb.velocity.y}");
            return;
        }

        // Calculate the target position to ensure the cube is properly aligned vertically
        Vector3 targetPosition = new Vector3(previousCube.position.x, previousCube.position.y + previousCube.localScale.y / 2 + cube.localScale.y / 2, previousCube.position.z);

        // Ensure the cube is aligned with the previous cube vertically
        if (Vector3.Distance(cube.position, targetPosition) > alignmentThreshold)
        {
            Debug.Log($"{cube.name} is not properly aligned vertically. Applying downward force to settle.");
            rb.AddForce(Vector3.down * 10f); // Apply a small downward force to help it settle
            return;
        }

        // Once the cube is properly settled and aligned, lock it in place
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.mass = 10000f; // Set mass to a very high value to prevent movement

        // Freeze all constraints to ensure the cube stays in place
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // Mark the cube as locked
        if (cube == baseCube)
        {
            baseCubeLocked = true;
        }
        else if (cube == midCube)
        {
            midCubeLocked = true;
        }
        else if (cube == topCube)
        {
            topCubeLocked = true;
        }

        Debug.Log($"{cube.name} has been locked in place.");
    }




    private void CompleteTrial()
    {
        trialActive = false;
        SaveResults();
        Debug.Log("Trial completed.");
    }

    public void EndTrialEarly()
    {
        // End the trial early if needed, recording current progress
        Debug.Log("Trial ended early.");
        CompleteTrial();
    }

    private void SaveResults()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(path, "stacking_results_" + timestamp + ".csv");

        try
        {
            File.WriteAllLines(filePath, stackingResults);
            Debug.Log("Results saved successfully at " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save results: " + e.Message);
        }

        stackingResults.Clear();
        stackingResults.Add("Step,Cube,PositionX,PositionY,PositionZ,AlignmentWithPrevious,AlignmentWithGuideline,RotationAlignment,Timestamp");
    }

    private void ResetCubes()
    {
        baseCube.position = baseInitialPosition;
        baseCube.rotation = baseInitialRotation;
        midCube.position = midInitialPosition;
        midCube.rotation = midInitialRotation;
        topCube.position = topInitialPosition;
        topCube.rotation = topInitialRotation;

        baseCubeLocked = false;
        midCubeLocked = false;
        topCubeLocked = false;

        currentStep = 0;

        // Reset the color of the cubes
        ChangeCubeColor(baseCube, Color.white);
        ChangeCubeColor(midCube, Color.white);
        ChangeCubeColor(topCube, Color.white);

        // Unlock the cubes' rigidbodies
        UnlockCube(baseCube);
        UnlockCube(midCube);
        UnlockCube(topCube);

        Debug.Log("Cubes reset to initial positions.");
    }

    private void UnlockCube(Transform cube)
    {
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 1f; // Restore the mass to a normal value
        rb.constraints = RigidbodyConstraints.None;
    }
}
