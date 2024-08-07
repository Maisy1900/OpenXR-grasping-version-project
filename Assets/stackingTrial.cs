using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

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
    private const float alignmentThreshold = 0.025f; // 25mm threshold
    private const float rotationThreshold = 25f; // 25 degrees threshold
    private const float velocityThreshold = 0.01f; // Velocity threshold to consider the cube settled
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
        if (Input.GetKeyDown(KeyCode.Y) && !trialActive)
        {
            StartTrial();
        }

        if (trialActive)
        {
            CheckAlignment();
        }
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
                alignmentWithPrevious = Vector3.Distance(currentCube.position, previousCube.position);
                break;
            case 3:
                currentCube = topCube;
                previousCube = midCube;
                isCurrentCubeLocked = topCubeLocked;
                alignmentWithPrevious = Vector3.Distance(currentCube.position, previousCube.position);
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

        // Calculate position alignment
        float alignmentWithGuideline = Vector3.Distance(currentCube.position, planeGuideline.position);
        float rotationAlignment = Quaternion.Angle(currentCube.rotation, planeGuideline.rotation);

        Rigidbody rb = currentCube.GetComponent<Rigidbody>();

        // Debugging information to understand the alignment and velocity
        Debug.Log($"{currentCube.name} - Alignment with Previous: {alignmentWithPrevious}, Alignment with Guideline: {alignmentWithGuideline}, Rotation Alignment: {rotationAlignment}, Velocity: {rb.velocity.magnitude}");

        // Check if the cube is aligned correctly
        bool isAligned = (currentStep == 1 && alignmentWithGuideline <= alignmentThreshold && rotationAlignment <= rotationThreshold) ||
                         (currentStep == 2 && alignmentWithPrevious <= alignmentThreshold && rotationAlignment <= rotationThreshold) ||
                         (currentStep == 3 && alignmentWithPrevious <= alignmentThreshold && rotationAlignment <= rotationThreshold);

        // Detailed debug logs
        if (currentStep == 1)
        {
            Debug.Log($"{currentCube.name} Alignment with Guideline: {alignmentWithGuideline} <= {alignmentThreshold}");
            Debug.Log($"{currentCube.name} Rotation Alignment: {rotationAlignment} <= {rotationThreshold}");
        }
        else
        {
            Debug.Log($"{currentCube.name} Alignment with Previous: {alignmentWithPrevious} <= {alignmentThreshold}");
            Debug.Log($"{currentCube.name} Rotation Alignment: {rotationAlignment} <= {rotationThreshold}");
        }

        // If the cube is aligned, has low velocity, and is not parented, lock it in place
        if (isAligned && rb.velocity.magnitude <= velocityThreshold && currentCube.parent == null)
        {
            Debug.Log($"{currentCube.name} is in alignment and settled.");
            RecordAlignment(currentCube, alignmentWithPrevious, alignmentWithGuideline, rotationAlignment);
            LockCube(currentCube);
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

    private void RecordAlignment(Transform cube, float alignmentWithPrevious, float alignmentWithGuideline, float rotationAlignment)
    {
        float timestamp = Time.time - trialStartTime;
        string result = $"{currentStep},{cube.name},{cube.position.x},{cube.position.y},{cube.position.z},{alignmentWithPrevious},{alignmentWithGuideline},{rotationAlignment},{timestamp}";
        stackingResults.Add(result);
        Debug.Log($"Step {currentStep} completed for {cube.name}. Time: {timestamp} seconds, Alignment with previous: {alignmentWithPrevious}m, Alignment with guideline: {alignmentWithGuideline}m, Rotation alignment: {rotationAlignment} degrees");
    }

    private void LockCube(Transform cube)
    {
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.mass = 10000f; // Set mass to a very high value to prevent movement

        // Optionally, you can also freeze the position and rotation to ensure the cube stays in place
        rb.constraints = RigidbodyConstraints.FreezeAll;

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
    }
}
