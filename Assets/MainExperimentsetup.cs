using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MainExperimentsetup : MonoBehaviour
{
    // Variables to store experiment data
    private List<Vector3> cubePositions = new List<Vector3>();
    private List<string> handPoses = new List<string>();
    private List<float> timings = new List<float>();
    private string csvFilePath = "experiment_results.csv";

    public Animator handAnimator; // Reference to the Animator component
    public GameObject rWristGameObject; // Reference to the GameObject with ArticulationDriver
    private ArticulationDriver articulationDriver; // Reference to the ArticulationDriver script


    // Function to record results
    void RecordResults(Vector3 cubePosition, string handPose, float timing)
    {
        cubePositions.Add(cubePosition);
        handPoses.Add(handPose);
        timings.Add(timing);

        // Write results to CSV file
        using (StreamWriter writer = new StreamWriter(csvFilePath, true))
        {
            writer.WriteLine($"{cubePosition.x},{cubePosition.y},{cubePosition.z},{handPose},{timing}");
        }
    }

    // Function to conduct trials
    IEnumerator ConductTrials()
    {
        for (int i = 0; i < 10; i++) // Example: 10 trials
        {
            // TODO: Change physics parameters here based on the defined metric

            // TODO: Run hand animations for each trial

            // Wait for trial to complete (example: 5 seconds per trial)
            yield return new WaitForSeconds(5.0f);

            // TODO: Record results after each trial
            Vector3 cubePosition = Vector3.zero; // Placeholder
            string handPose = "default"; // Placeholder
            float timing = 0.0f; // Placeholder
            RecordResults(cubePosition, handPose, timing);
        }
    }

    // Function to run hand calibration
    IEnumerator RunHandCalibration()
    {
        // Start the hand calibration animation
        handAnimator.Play("Calibration hand");

        // Wait for 2 seconds
        yield return new WaitForSeconds(2.0f);

        // Directly trigger the calibration logic that would normally be triggered by pressing "M"
   

        // Stop the calibration animation
        handAnimator.StopPlayback();

        // Calibration logic is over
        Debug.Log("Calibration is over");
    }

    // Function to prepare fitness rating/outcome based on CSV
    void PrepareFitnessRating()
    {
        // TODO: Read the CSV file and calculate fitness ratings
        using (StreamReader reader = new StreamReader(csvFilePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] data = line.Split(',');
                // Process data here
            }
        }
    }

    // Function to run hand animations for each trial
    void RunHandAnimations()
    {
        // TODO: Implement hand animations here
    }

    // Start is called before the first frame update
    void Start()
    {
        // Reference the Animator component
        handAnimator = GetComponent<Animator>();

        // Find the ArticulationDriver script on the specified GameObject
        if (rWristGameObject != null)
        {
            articulationDriver = rWristGameObject.GetComponent<ArticulationDriver>();

            // Check if the ArticulationDriver script is found
            if (articulationDriver == null)
            {
                Debug.LogError("ArticulationDriver component not found on the specified GameObject!");
            }
        }
        else
        {
            Debug.LogError("R_Wrist GameObject reference is not set!");
        }

        // Start the calibration coroutine
        StartCoroutine(RunHandCalibration());
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Any per-frame updates
    }
}
